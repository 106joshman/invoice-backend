using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InvoiceService.Services;

public class AuthService(ApplicationDbContext context, IConfiguration configuration)
{
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    // public async Task<AuthResponseDto> Register(CreateUserDto createUserDto)
    // {
    //     var emailExists = await _context.Users.AnyAsync(u => u.Email == createUserDto.Email && !u.IsDeleted);

    //     if (string.IsNullOrWhiteSpace(createUserDto.Email) || string.IsNullOrWhiteSpace(createUserDto.Password))
    //     {
    //         throw new Exception("Email and password are required");
    //     }

    //     // // CHECK IF EMAIL ALREADY EXISTS
    //     if (emailExists)
    //     {
    //         throw new UnauthorizedAccessException("Email already in use.");
    //     }

    //     // HASH PASSWORD BEFORE STORING IN DATABASE
    //     var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password, workFactor: 10);

    //     // CREATE USER OBJECT BEFOR SENDING TO DATABASE
    //     var user = new User
    //     {
    //         FullName = createUserDto.FullName,
    //         Email = createUserDto.Email,
    //         Role = "User",
    //         Password = passwordHash,
    //     };

    //     _context.Users.Add(user);
    //     await _context.SaveChangesAsync();

    //     // GENERATE JWT TOKEN
    //     var token = GenerateJwtToken(user, businessUser = null);

    //     return new AuthResponseDto
    //     {
    //         Token = token,
    //         UserId = user.Id,
    //         FullName = user.FullName,
    //         Email = user.Email,
    //         Role = user.Role,
    //         BusinessId = user.BusinessId,
    //         BusinessRole = user.BusinessRole,
    //         CreatedAt = user.CreatedAt
    //     };
    // }

    public async Task<AuthResponseDto> Login(UserLoginDto loginDto)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == loginDto.Email.ToLower() &&
                !x.IsDeleted)
            ?? throw new UnauthorizedAccessException("User not found.");

        // CHECK IF USER IS ASSIGNED TO AN ACTIVE BUSINESS
        var businessUser = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == user.Id &&
                bu.IsActive &&
                bu.IsVerified &&
                !bu.IsDeleted &&
                !bu.Business.IsDeleted)
            ?? throw new UnauthorizedAccessException("Access denied.");

        // ❌ CHECK FOR SUSPENDED ACCOUNT
        if (!businessUser.IsActive)
            throw new UnauthorizedAccessException("Your account has been suspended.");

        // ❌ CHECK FOR VALID EMAIL AND VERIFY PASSWORD
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        // ✅ First login → mark verified
        if (!businessUser.IsVerified)
        {
            businessUser.IsVerified = true;
            await _context.SaveChangesAsync();
        }

        // GENERATE JWT TOKEN
        var token = GenerateJwtToken(user, businessUser);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,

            BusinessId = businessUser.BusinessId,
            BusinessRole = businessUser.Role,
            IsVerified = businessUser.IsVerified,

            CreatedAt = user.CreatedAt
        };
    }

    private string GenerateJwtToken(User user, BusinessUser businessUser)
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

        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7), // EXPRES IN 7 DAYS
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}