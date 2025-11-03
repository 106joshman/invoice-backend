using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InvoiceService.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> Register(CreateUserDto createUserDto)
    {
        if (string.IsNullOrWhiteSpace(createUserDto.Email) || string.IsNullOrWhiteSpace(createUserDto.Password))
        {
            throw new Exception("Email and password are required");
        }

        // // CHECK IF EMAIL ALREADY EXISTS
        if (await _context.Users.AnyAsync(x => x.Email == createUserDto.Email))
        {
            throw new UnauthorizedAccessException("Email already exists");
        }

        // HASH PASSWORD BEFORE STORING IN DATABASE
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password, workFactor: 10);

        // CREATE USER OBJECT BEFOR SENDING TO DATABASE
        var user = new User
        {
            FullName = createUserDto.FullName,
            Email = createUserDto.Email,
            Role = "User",
            Password = passwordHash,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // GENERATE JWT TOKEN
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AuthResponseDto> Login(UserLoginDto loginDto)
    {
        // Console.WriteLine($"Received Login request for {loginDto.Email}");
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == loginDto.Email.ToLower());

         // CHECK FOR VALID EMAIL AND VERIFY PASSWORD
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
        {
            // Console.WriteLine("User not found");
            throw new Exception("Invalid email or password");
        }

        // GENERATE JWT TOKEN
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FullName}"),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
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