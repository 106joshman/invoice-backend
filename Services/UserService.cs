using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Helpers;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class UserService(ApplicationDbContext context, EmailService _emailService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly EmailService _emailService = _emailService;

    public async Task<UserResponseDto> GetUserProfile(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");

        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDto> UpdateUserAsync(Guid userId, UserUpdateDto userUpdateDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrWhiteSpace(userUpdateDto.FullName))
            user.FullName = userUpdateDto.FullName;
        if (!string.IsNullOrWhiteSpace(userUpdateDto.Email))
            user.Email = userUpdateDto.Email;
        if (!string.IsNullOrWhiteSpace(userUpdateDto.PhoneNumber))
            user.PhoneNumber = userUpdateDto.PhoneNumber;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<PaginatedResponse<UserResponseDto>> GetAllUsers(PaginationParams paginationParams,
        string? FullName = null,
        string? email = null,
        string? RoleFilter = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(FullName))
            query = query.Where(u => u.FullName.Contains(FullName));

        // if (!string.IsNullOrWhiteSpace(BusinessName))
        //     query = query.Where(u => u.BusinessName.Contains(BusinessName));

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(u => u.Email.Contains(email));

        if (!string.IsNullOrWhiteSpace(RoleFilter))
            query = query.Where(u => u.Role == RoleFilter);

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<UserResponseDto>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
        };
    }

    public async Task<PaginatedResponse<UserResponseDto>> GetRegularUsers(PaginationParams paginationParams,
        string? FullName = null,
        string? BusinessName = null,
        string? Email = null)
        {
            return await GetAllUsers(paginationParams, FullName, Email, "User");
        }

    public async Task<PaginatedResponse<UserResponseDto>> GetAdminsAndSuperAdmins(PaginationParams paginationParams,
    string? FullName = null,
    string? Email = null)
    {
        var query = _context.Users.AsQueryable();

        // Filter for Admins and Super_Admins
        if (!string.IsNullOrWhiteSpace(FullName))
            query = query.Where(u => u.FullName.Contains(FullName));

        if (!string.IsNullOrWhiteSpace(Email))
            query = query.Where(u => u.Email.Contains(Email));

        query = query.Where(u => u.Role == "Admin" || u.Role == "Super_Admin");

        var totalCount = await query.CountAsync();

        var admins = await query
            .OrderBy(u => u.FullName)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<UserResponseDto>
        {
            Items = admins,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
        };
    }

    public async Task ChangePassword(Guid userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");

        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.Password))
        {
            throw new Exception("Current password is incorrect!");
        }

        if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword) || changePasswordDto.NewPassword.Length < 8)
        {
            throw new Exception("New password must be at least 8 characters long!");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword, workFactor: 8);

        await _context.SaveChangesAsync();
    }
}