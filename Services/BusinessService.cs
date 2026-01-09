using InvoiceService.Data;
using InvoiceService.DTOs;
using InvoiceService.Helpers;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class BusinessService (ApplicationDbContext context, EncryptionHelper encryptionHelper)
{
    private readonly ApplicationDbContext _context = context;
    private readonly EncryptionHelper _encryptionHelper = encryptionHelper;

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
            MonthlyInvoiceCount = business.MonthlyInvoiceCount,
            IsMultiTenant = business.IsMultiTenant,
            BrandColor = business.BrandColor,
            CompanyLogoUrl = business.CompanyLogoUrl,
            CreatedAt = business.CreatedAt,
            PaymentInfo = await _context.PaymentInfo
                .AsNoTracking()
                .Where(p => p.BusinessId == business.Id)
                .Select(p => new PaymentInfoResponseDto
                {
                    Id = p.Id,
                    BankName = p.BankName,
                    AccountName = p.AccountName,
                    AccountNumber = _encryptionHelper.Decrypt(p.AccountNumber),
                    RoutingNumber = p.RoutingNumber,
                    SwiftCode = p.SwiftCode,
                    IBAN = p.IBAN,
                    PaymentTerms = p.PaymentTerms
                })
                .FirstOrDefaultAsync(),

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

    public async Task<PaginatedResponse<BusinessResponseDto>> GetAllBusinessAsync(
        string requestingUserRole,
        Guid requestingUserId,
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

    public async Task<PaginatedResponse<BusinessTeamMemberDto>> GetBusinessTeamAsync(
        Guid requesterUserId,
        PaginationParams paginationParams,
        string? search)
    {
        // Verify requester
        var requester = await _context.BusinessUsers
            .Include(bu => bu.Business)
            .Include(bu => bu.User)
            .FirstOrDefaultAsync(bu =>
                bu.UserId == requesterUserId &&
                bu.IsActive &&
                !bu.Business.IsDeleted)
            ?? throw new UnauthorizedAccessException("Access denied.");

        if (requester.Role != "Owner" && requester.Role != "Admin")
            throw new UnauthorizedAccessException("Insufficient permission.");

        var query = _context.BusinessUsers
            .AsNoTracking()
            .Include(bu => bu.User)
            .Where(bu =>
                bu.BusinessId == requester.BusinessId &&
                !bu.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim().ToLower();

            query = query.Where(bu =>
                bu.User.FullName.Contains(search) ||
                bu.User.Email.ToLower().Contains(search));
        }

        var totalCount= await query.CountAsync();

        var items = await query
            .OrderBy(bu => bu.User.FullName)
            .Skip((paginationParams.PageNumber -1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(bu => new BusinessTeamMemberDto
            {
                BusinessUserId = bu.Id,
                UserId = bu.UserId,
                FullName = bu.User.FullName,
                Email = bu.User.Email,
                Role = bu.Role,
                IsActive = bu.IsActive,
                IsVerified = bu.IsVerified,
                CreatedAt = bu.User.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResponse<BusinessTeamMemberDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize
        };
    }

    public async Task ToggleBusinessUserStatusAsync(
        Guid adminUserId,
        Guid targetUserId,
        bool activate)
    {
        var admin = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu =>
                bu.UserId == adminUserId &&
                bu.IsActive)
            ?? throw new UnauthorizedAccessException("Access denied.");

        if (admin.Role != "Owner" && admin.Role != "Admin")
            throw new UnauthorizedAccessException("Insufficent permission");

        var target = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu =>
                bu.UserId == targetUserId &&
                bu.BusinessId == admin.BusinessId)
            ?? throw new Exception("User not found");

        // 🚫 Prevent self-deactivation
        if (target.UserId == adminUserId)
            throw new InvalidOperationException("You cannot modify your own status.");

        if (target.Role == "Owner")
            throw new Exception("Owner account cannot be suspended.");

        target.IsActive = activate;
        target.IsVerified = activate;

        _context.AuditLogs.Add(new AuditLog
        {
            Action = activate ? "ACTIVATE_USER" : "SUSPEND_USER",
            EntityName = "BUSINESS_USER",
            EntityId = targetUserId,
            UserId = adminUserId,
            ChangeBy = adminUserId.ToString()
        });

        await _context.SaveChangesAsync();
    }

    public async Task ChangeUserRoleAsync(
        Guid adminUserId,
        Guid targetUserId,
        string newRole)
    {
        var allowedRoles = new[] { "Admin", "Member", "Staff" };

        if (!allowedRoles.Contains(newRole))
            throw new Exception("Invalid role");

        var admin = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu =>
                bu.UserId == adminUserId &&
                bu.Role == "Owner" &&
                bu.IsActive)
            ?? throw new UnauthorizedAccessException("Only owners can change roles.");

        var target = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu =>
                bu.UserId == targetUserId &&
                bu.BusinessId == admin.BusinessId)
            ?? throw new Exception("User not found");

        // 🚫 Prevent changing own role
        if (target.UserId == adminUserId)
            throw new InvalidOperationException("You cannot change your own role.");

        // 🚫 Prevent role change on owner
        if (target.Role == "Owner")
            throw new InvalidOperationException("Owner role cannot be modified.");

        if (target.Role == newRole)
            throw new InvalidOperationException("User already has this role.");

        target.Role = newRole;

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "CHANGE_ROLE",
            EntityName = "BUSINESS_USER",
            EntityId = targetUserId,
            UserId = adminUserId,
            ChangeBy = adminUserId.ToString()
        });

        await _context.SaveChangesAsync();
    }
}