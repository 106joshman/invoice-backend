using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class BusinessService (ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    // BUSINESS LOGIC HERE
    private async Task<bool> HasAccessToBusinessAsync(Guid userId, string role, Guid businessId)
    {
        // SUPER ADMIN AND ADMIN HAVE FULL ACCESS
        if (role.Equals("super_admin", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // CHECK IF USER IS AN ACTIVE MEMBER OF THE BUSINESS
        return await _context.BusinessUsers.AnyAsync(bu =>
            bu.UserId == userId &&
            bu.BusinessId == businessId &&
            bu.IsActive &&
            !bu.IsDeleted);
    }
    public async Task<BusinessResponseDto> GetBusinessByIdAsync(
        Guid businessId,
        Guid requestingUserId,
        string requestingUserRole)
    {
        var hasAccess = await HasAccessToBusinessAsync(requestingUserId, requestingUserRole, businessId);

        if (!hasAccess)
            throw new UnauthorizedAccessException(
                "You do not have access to this business.");

        var business = await _context.Businesses
            .AsNoTracking()
            .Include(b => b.BusinessUsers)
                .ThenInclude(bu => bu.User)
            .FirstOrDefaultAsync(b => b.Id == businessId && !b.IsDeleted)
            ?? throw new Exception("Business not found");

        return new BusinessResponseDto
        {
            Id = business.Id,
            Name = business.Name,
            Address = business.Address,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            SubscriptionPlan = business.SubscriptionPlan,
            IsMultiTenant = business.IsMultiTenant,
            BrandColor = business.BrandColor,
            CompanyLogoUrl = business.CompanyLogoUrl,
            CreatedAt = business.CreatedAt,

            TeamMembers = [.. business.BusinessUsers
                .Where(bu => !bu.IsDeleted)
                .Select(bu => new BBusinessMemberDto
                {
                    UserId = bu.UserId,
                    FullName = bu.User.FullName,
                    Email = bu.User.Email,
                    Role = bu.Role,
                    IsVerified = bu.IsVerified,
                    IsActive = bu.IsActive,
                    JoinedAt = bu.JoinedAt
                })]
        };
    }

    public async Task<PaginatedResponse<BusinessResponseDto>> GetAllBusinessAsync(string requestingUserRole, Guid requestingUserId,
    PaginationParams paginationParams,
    string? Name,
    string? SubscriptionPlan)
    {
        var requester = await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == requestingUserId && !u.IsDeleted)
        ?? throw new UnauthorizedAccessException("Invalid user");

        // SUPER ADMIN AND ADMIN CAN ACCESS ALL BUSINESSES
        if (!requestingUserRole.Equals("super_admin", StringComparison.OrdinalIgnoreCase) &&
            !requestingUserRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to access all businesses.");
        }

        var query = _context.Businesses
            .AsNoTracking()
            .Where(b => !b.IsDeleted);

        if (!string.IsNullOrEmpty(Name))
        {
            query = query.Where(b => b.Name.Contains(Name));
        }

        if (!string.IsNullOrEmpty(SubscriptionPlan))
        {
            query = query.Where(b => b.SubscriptionPlan == SubscriptionPlan);
        }

        var totalCount = await query.CountAsync();

        var businessLists = await query
            .OrderBy(b => b.CreatedAt)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(business => new BusinessResponseDto
            {
                Id = business.Id,
                Name = business.Name,
                Address = business.Address,
                Email = business.Email,
                PhoneNumber = business.PhoneNumber,
                SubscriptionPlan = business.SubscriptionPlan,
                IsMultiTenant = business.IsMultiTenant,
                CompanyLogoUrl = business.CompanyLogoUrl,
                CreatedAt = business.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<BusinessResponseDto>
        {
            Items = businessLists,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
        };
    }
}