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
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;

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
            .FirstOrDefaultAsync(a =>
            a.Id == adminUserId &&
            (a.Role.ToLower() == "super_admin" || a.Role.ToLower() == "Admin"))
            ?? throw new UnauthorizedAccessException("Only admins can create businesses");

        var normalizedUserEmail = NormalizeEmail(registrationDto.Email);
        var normalizedBusinessEmail = NormalizeEmail(registrationDto.BusinessEmail);

        // VERIFY IF EMAIL ALREADY EXISTS
        if (await _context.Users.AnyAsync(u => u.Email == normalizedUserEmail))
            throw new InvalidOperationException("Email already exist.");

        // CREATING AN ADMIN USER FOR THE BUSINESS,
        var business = new Business
        {
            Name = registrationDto.BusinessName,
            Address = registrationDto.BusinessAddress,
            PhoneNumber = registrationDto.PhoneNumber,
            IsMultiTenant = registrationDto.IsMultiTenant,
            Email = normalizedBusinessEmail,
            SubscriptionPlan = "Free",
        };

        _context.Businesses.Add(business);

        // CREATE BUSINESS OWNER USER
        var businessOwner = new User
        {
            FullName = registrationDto.FullName,
            Email = normalizedUserEmail,
            Role = "User",
            IsPasswordSet = false,
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
            Action = "CREATE_BUSINESS",
            EntityName = "BUSINESS",
            EntityId = business.Id,
            UserId = adminUserId,
            ChangeBy = "SYSTEM_ADMIN"
        });

        await _context.SaveChangesAsync();

        var emailSent = await SendSetPasswordLinkAsync(businessOwner);

        // RECORD EMAIL STATUS
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
                ? "Business created. Set password link sent."
                : "Business created. Email failed — resend activitation."
        };
    }

    public async Task<AuthResponseDto> Login(UserLoginDto loginDto)
    {
        var normalizedUserEmail = NormalizeEmail(loginDto.Email);
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == normalizedUserEmail.ToLower() &&
                !x.IsDeleted)
            ?? throw new UnauthorizedAccessException("User not found.");

        // ❌ VERIFY PASSWORD
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

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
                PhoneNumber = user.PhoneNumber,
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
            PhoneNumber = user.PhoneNumber,
            BusinessId = businessUser.BusinessId,
            BusinessRole = businessUser.Role,
            IsActive = businessUser.IsActive,
            RequirePasswordChange = false,
            IsVerified = businessUser.IsVerified,
            CreatedAt = user.CreatedAt,
            Message = "Login successful"
        };
    }

    public async Task InviteBusinessUserAsync(
        Guid inviterUserId,
        InviteBusinessUserDto inviteBusinessUserDto)
    {
        var normalizedUserEmail = NormalizeEmail(inviteBusinessUserDto.Email);
        // VERIFY BUSINESS OWNER OR ADMIN FOR EVERY INVITE
        var inviter = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .Include(bu => bu.User)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == inviterUserId &&
                bu.IsActive &&
                !bu.Business.IsDeleted)
            ?? throw new UnauthorizedAccessException("Access denied");

        if (inviter.Role != "Owner" && inviter.Role != "Admin")
            throw new UnauthorizedAccessException("Only Onwers Admins can invite users.");

        var business = inviter.Business;

        // 3️⃣ CHECK USER PLAN FOR SEAT LIMIT
        // int activeUsersCount = await _context.BusinessUsers
        //     .CountAsync(bu => bu.BusinessId == business.Id && bu.IsActive);

        // int maxSeats = business.SubscriptionPlan switch
        // {
        //     "Free" => 1,
        //     "Pro" => 1,
        //     "Business" => 5,
        //     _ => 1
        // };

        // if (activeUsersCount >= maxSeats)
        //     throw new InvalidOperationException("User seat limit reached for your plan.");

        // VERIFY IF EMAIL ALREADY EXISTS
        if (await _context.Users.AnyAsync(u =>
            u.Email == normalizedUserEmail &&
            !u.IsDeleted))
            throw new InvalidOperationException("Email already exist.");

        // CREATE BUSINESS TEAM
        var newUser = new User
        {
            FullName = inviteBusinessUserDto.FullName,
            Email = normalizedUserEmail,
            PhoneNumber = inviteBusinessUserDto.PhoneNumber,
            Role = "User",
            IsPasswordSet = false,
            IsDeleted = false
        };

        _context.Users.Add(newUser);

        var businessUser = new BusinessUser
        {
            BusinessId = business.Id,
            User = newUser,
            Role = inviteBusinessUserDto.BusinessRole,
            IsActive = true,
            IsVerified = false
        };

        // ✅ LINK USER TO BUSINESS
        _context.BusinessUsers.Add(businessUser);

        // ✅ AUDIT LOG
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "INVITE",
            EntityName = "BUSINESS_USER",
            EntityId = newUser.Id,
            UserId = inviterUserId,
            ChangeBy = inviter.UserId.ToString()
        });

        // 🔐 SAVE CORE DATA FIRST (IMPORTANT)
        await _context.SaveChangesAsync();
        var emailSent = await SendSetPasswordLinkAsync(newUser);

        // ✅ RECORD EMAIL STATUS
        newUser.CredentialsEmailSent = emailSent;

        await _context.SaveChangesAsync();
    }

    public async Task ResendBusinessCredentialsAsync(
        Guid userId,
        Guid adminId)
    {
        await EnforceCredentialResetLimits(adminId, userId);

        var targetBusinessUser = await _context.BusinessUsers
            .Include(bu => bu.User)
            .Include(bu => bu.Business)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted)
            ?? throw new Exception("Business user not found.");

        await EnsureCanResetPassword(adminId, targetBusinessUser);

        var emailSent = await SendSetPasswordLinkAsync(targetBusinessUser.User);

        targetBusinessUser.User.CredentialsEmailSent = emailSent;

        // AUDIT LOG ENTRY
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "RESET_PASSWORD_LINK",
            EntityName = "BUSINESS_USER",
            EntityId = targetBusinessUser.UserId,
            UserId = adminId,
            ChangeBy = "SYSTEM_ADMIN"
        });

        await _context.SaveChangesAsync();
    }

    public async Task SetPassword(SetPasswordDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == dto.UserId)
            ?? throw new Exception("User not found");

        // ✅ Validate token existence
        if (user.PasswordResetTokenHash == null ||
            user.PasswordResetTokenExpiresAt == null)
        {
            throw new UnauthorizedAccessException("Reset token invalid or already used.");
        }

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Reset token expired");

        if (!Verify(dto.Token, user.PasswordResetTokenHash!))
            throw new UnauthorizedAccessException("Invalid reset token");

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 8);
        user.IsPasswordSet = true;
        user.PasswordChangedAt = DateTime.UtcNow;

        // INVALIDATE TOKEN
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "SET_PASSWORD",
            EntityName = "USER",
            EntityId = user.Id,
            UserId = user.Id,
            ChangeBy = user.Id.ToString()
        });

        await _context.SaveChangesAsync();
    }

    private async Task<bool> SendSetPasswordLinkAsync(User user)
    {
        try
        {
            var rawToken = GenerateRawToken();

            user.PasswordResetTokenHash = HashToken(rawToken);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);

            var link =
                $"{_configuration["Frontend:BaseUrl"]}/auth/set-password"+
                $"?userId={user.Id}&token={Uri.EscapeDataString(rawToken)}";

            var businessName = await _context.BusinessUsers
                .Where(bu => bu.UserId == user.Id)
                .Select(bu => bu.Business.Name)
                .FirstOrDefaultAsync() ?? "Your Business";

            await _emailService.SendWelcomeSetPasswordEmailAsync(
                user.Email,
                user.FullName,
                businessName,
                link
            );

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Set password email failed: {ex.Message}");
            return false;
        }
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

    public static string GenerateRawToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public static string HashToken(string rawToken)
    {
        return BCrypt.Net.BCrypt.HashPassword(rawToken);
    }

    public static bool Verify(string rawToken, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(rawToken, hash);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");

        return email.Trim().ToLowerInvariant();
    }

    public async Task EnforceCredentialResetLimits(Guid adminId, Guid targetUserId)
    {
        var now = DateTime.UtcNow;

        // 1️⃣ Target user limit (3 per 24h)
        var userResets = await _context.AuditLogs.CountAsync(a =>
            a.EntityId == targetUserId &&
            a.Action == "RESEND_PASSWORD" &&
            a.Timestamp >= now.AddHours(-24));

        if (userResets >= 3)
            throw new Exception("This user has reached the maximum credential resets today.");

        // 2️⃣ Admin limit (10 per hour)
        var adminResets = await _context.AuditLogs.CountAsync(a =>
            a.UserId == adminId &&
            a.Action == "RESEND_PASSWORD" &&
            a.Timestamp >= now.AddHours(-1));

        if (adminResets >= 10)
            throw new Exception("You have reached the maximum reset attempts per hour.");

        // 3️⃣ Cooldown (5 minutes)
        var lastReset = await _context.AuditLogs
            .Where(a =>
                a.UserId == adminId &&
                a.EntityId == targetUserId &&
                a.Action == "RESEND_PASSWORD")
            .OrderByDescending(a => a.Timestamp)
            .Select(a => a.Timestamp)
            .FirstOrDefaultAsync();

        if (lastReset != default && lastReset >= now.AddMinutes(-5))
            throw new Exception("Please wait before sending another reset.");
    }

    private async Task EnsureCanResetPassword(
        Guid adminId,
        BusinessUser targetBusinessUser)
    {
        // 🔐 Load requester
        var requester = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .Include(bu => bu.User)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == adminId &&
                bu.IsActive &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted);

        // SYSTEM ADMIN (not tied to business)
        if (requester == null)
        {
            var systemAdmin = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == adminId &&
                (u.Role == "super_admin" || u.Role == "admin"));

            if (systemAdmin == null)
                throw new UnauthorizedAccessException("Unauthorized action.");

            // ✅ System admin allowed
            return;
        }

        // 🔐 Must be same business
        if (requester.BusinessId != targetBusinessUser.BusinessId)
            throw new UnauthorizedAccessException("Cross-business access denied.");

        // 🔐 Role check
        if (requester.Role != "Owner" && requester.Role != "Admin")
            throw new UnauthorizedAccessException("Only Owners or Admins can reset passwords.");

        // 🔐 Prevent self-reset abuse (optional)
        if (requester.UserId == targetBusinessUser.UserId)
            throw new UnauthorizedAccessException("You cannot reset your own password.");
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