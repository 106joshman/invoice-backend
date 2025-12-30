using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Helpers;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InvoiceService.Services;

public class AuthService(ApplicationDbContext context, IConfiguration configuration, EmailService _emailService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;
    private readonly EmailService _emailService = _emailService;

    public async Task<BusinessRegistrationResponseDto> RegisterBusinessAsync(
        BusinessRegistrationRequestDto registrationDto,
        Guid adminUserId)
    {
        var superAdmin = await _context.Users
            .Where(a =>
            a.Id == adminUserId &&
            (a.Role.ToLower() == "super_admin" || a.Role.ToLower() == "Admin"))
            .FirstOrDefaultAsync()
        ?? throw new UnauthorizedAccessException("Only admins can create businesses");

        // VERIFY IF EMAIL ALREADY EXISTS
        if (await _context.Users.AnyAsync(u => u.Email == registrationDto.Email))
            throw new InvalidOperationException("Email already exist.");

        // CREATING AN ADMIN USER FOR THE BUSINESS,
        var business = new Business
        {
            Name = registrationDto.BusinessName,
            Address = registrationDto.BusinessAddress,
            PhoneNumber = registrationDto.PhoneNumber,
            IsMultiTenant = registrationDto.IsMultiTenant,
            SubscriptionPlan = "Free",
        };

        _context.Businesses.Add(business);

        string tempPassword = PasswordGenerator.GenerateTemporaryPassword();

        // CREATE BUSINESS OWNER USER
        var businessOwner = new User
        {
            FullName = registrationDto.FullName,
            Email = registrationDto.Email,
            Role = "User",
            Password = BCrypt.Net.BCrypt.HashPassword(tempPassword, workFactor: 10),
            IsTemporaryPassword = true,
            TempPasswordGeneratedAt = DateTime.UtcNow,
            CredentialsEmailSent = false,
        };

        _context.Users.Add(businessOwner);

        // LINK BUSINESS OWNER TO BUSINESS
        var businessUser = new BusinessUser
        {
            Business = business,
            User = businessOwner,
            Role = "Owner",
            IsVerified = false,
            IsActive = true,
        };
        _context.BusinessUsers.Add(businessUser);

        // AUDIT LOG ENTRY
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "CREATE",
            EntityName = "BUSINESS",
            EntityId = business.Id,
            UserId = adminUserId,
            ChangeBy = "SYSTEM_ADMIN"
        });

        await _context.SaveChangesAsync();

        bool emailSent = true;

        try
        {
            // SEND TEMPORARY PASSWORD TO BUSINESS OWNER EMAIL
            await _emailService.SendWelcomeEmailAsync(
                toEmail: registrationDto.Email,
                fullName: registrationDto.FullName,
                businessName: registrationDto.BusinessName,
                temporaryPassword: tempPassword
            );
        }
        catch (Exception)
        {
            emailSent = false;
        }

        // ✅ RECORD EMAIL STATUS
        businessOwner.CredentialsEmailSent = emailSent;
        await _context.SaveChangesAsync();

        return new BusinessRegistrationResponseDto
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            BusinessAddress = business.Address,
            PhoneNumber = business.PhoneNumber,
            IsMultiTenant = business.IsMultiTenant,
            FullName = businessOwner.FullName,
            Email = businessOwner.Email,
            Message = emailSent
                ? "Business registered successfully. Credentials sent via email."
                : "Business registered successfully. Email delivery failed — please resend credentials."
        };
    }

    public async Task<AuthResponseDto> Login(UserLoginDto loginDto)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == loginDto.Email.ToLower() &&
                !x.IsDeleted)
            ?? throw new UnauthorizedAccessException("User not found.");

        // ❌ VERIFY PASSWORD
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.IsTemporaryPassword &&
            user.TempPasswordGeneratedAt.HasValue &&
            user.TempPasswordGeneratedAt.Value.AddHours(24) < DateTime.UtcNow)
        {
            throw new Exception("Temporary password has expired. Please request a new one.");
        }

        // 🔁 FORCE PASSWORD CHANGE
        if (user.IsTemporaryPassword)
        {
            var passwordChangeToken = GeneratePasswordChangeToken(user);

            return new AuthResponseDto
            {
                Token = passwordChangeToken,
                RequirePasswordChange = true,
                UserId = user.Id,
                Email = user.Email,
                Message = "Password change required."
            };
        }

        if (user.Role == "super_admin" || user.Role == "admin")
        {
            // GENERATE JWT TOKEN
            var token = GenerateSystemJwtToken(user);

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                Message = "Login successful"
            };
        }

        // CHECK IF USER IS ASSIGNED TO AN ACTIVE BUSINESS
        var businessUser = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == user.Id &&
                bu.IsActive &&
                bu.IsVerified &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted)
            ?? throw new UnauthorizedAccessException("Business access denied.");

        // ❌ CHECK FOR SUSPENDED ACCOUNT
        if (!businessUser.IsActive)
            throw new UnauthorizedAccessException("Your account has been suspended.");

        // VERIFY BUSINESS USER ON FIRST SUCCESSFUL LOGIN
        if (!businessUser.IsVerified)
        {
            businessUser.IsVerified = true;
            await _context.SaveChangesAsync();
        }

        // GENERATE JWT TOKEN
        var businessToken = GenerateBusinessJwtToken(user, businessUser);

        return new AuthResponseDto
        {
            Token = businessToken,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            BusinessId = businessUser.BusinessId,
            BusinessRole = businessUser.Role,
            IsVerified = businessUser.IsVerified,
            CreatedAt = user.CreatedAt,
            Message = "Login successful"
        };
    }

    public async Task ResendBusinessCredentialsAsync(Guid userId, Guid adminId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
            u.Id == userId)
            ?? throw new Exception("User not found");

        var businessName = await _context.BusinessUsers
            .Where(bu => bu.UserId == user.Id)
            .Select(bu => bu.Business.Name)
            .FirstOrDefaultAsync()
            ?? throw new Exception("Business not found for the user");

        string newTempPassword = PasswordGenerator.GenerateTemporaryPassword();

        user.Password = BCrypt.Net.BCrypt.HashPassword(newTempPassword);
        user.IsTemporaryPassword = true;
        user.TempPasswordGeneratedAt = DateTime.UtcNow;

        bool emailSent = true;

        try
        {
            await _emailService.SendWelcomeEmailAsync(
                user.Email,
                user.FullName,
                businessName,
                newTempPassword
            );
        }
        catch (Exception)
        {
            emailSent = false;
        }

        user.CredentialsEmailSent = emailSent;

        // AUDIT LOG ENTRY
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "RESET_PASSWORD",
            EntityName = "BUSINESS USER",
            EntityId = userId,
            UserId = adminId,
            ChangeBy = "SYSTEM_ADMIN"
        });
        await _context.SaveChangesAsync();
    }

    public async Task ForceChangePassword(Guid userId, ForceChangePasswordDto forceChangePasswordDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new Exception("User not found");

        user.Password = BCrypt.Net.BCrypt.HashPassword(forceChangePasswordDto.NewPassword, workFactor: 8);
        user.IsTemporaryPassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private string GenerateSystemJwtToken(User user)
    {
        var claims = new[]
        {
           new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FullName}"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            // SYSTEM ROLE
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("Scope", "System"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        return BuildToken(claims, expiresInMinutes: 60 * 8); // 8 HOURS
    }

    private string GenerateBusinessJwtToken(User user, BusinessUser businessUser)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FullName}"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            // SYSTEM ROLE
            new Claim(ClaimTypes.Role, user.Role),

            // BUSINESS CONTEXT CLAIMS
            new Claim("BusinessId", businessUser.BusinessId.ToString()),
            new Claim("BusinessRole", businessUser.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        return BuildToken(claims, expiresInMinutes: 60 * 24); // 24 HOURS
    }

    private string GeneratePasswordChangeToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("Scope", "PasswordChange"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return BuildToken(
            claims,
            expiresInMinutes: 15 // short-lived
        );
    }


    private string BuildToken(IEnumerable<Claim> claims, int expiresInMinutes)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}