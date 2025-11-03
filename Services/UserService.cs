using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class UserService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<UserResponseDto> GetUserProfile(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");

        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CompanyLogo = user.CompanyLogo,
            BusinessName = user.BusinessName,
            Role = user.Role,
            SubscriptionPlan = user.SubscriptionPlan,
            MonthlyInvoiceCount = user.MonthlyInvoiceCount,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<PaginatedResponse<UserResponseDto>> GetAllUsers(PaginationParams paginationParams,
        string? FullName = null,
        string? BusinessName = null,
        string? email = null,
        string? RoleFilter = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(FullName))
            query = query.Where(u => u.FullName.Contains(FullName));

        if (!string.IsNullOrWhiteSpace(BusinessName))
            query = query.Where(u => u.BusinessName.Contains(BusinessName));

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
                Address = u.Address,
                CompanyLogo = u.CompanyLogo,
                BusinessName = u.BusinessName,
                Role = u.Role,
                SubscriptionPlan = u.SubscriptionPlan,
                MonthlyInvoiceCount = u.MonthlyInvoiceCount,
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
            return await GetAllUsers(paginationParams, FullName, BusinessName, Email, "User");
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
                Address = u.Address,
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
}