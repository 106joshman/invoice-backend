using InvoiceService.Data;
using InvoiceService.Dtos;
using InvoiceService.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Services;

public class AuditLogService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PaginatedResponse<AuditLogResponseDto>> GetBusinessAuditLogsAsync(
        Guid userId,
        Guid businessId,
        PaginationParams paginationParams)
    {
        // AUTHORIZE USER
        var businessUser = await _context.BusinessUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(bu =>
                bu.UserId == userId &&
                bu.BusinessId == businessId &&
                bu.IsActive &&
                !bu.IsDeleted)
            ?? throw new UnauthorizedAccessException(
                "You are not authorized to view audit logs.");

        var query = _context.AuditLogs
        .AsNoTracking()
        .Include(a => a.User)
        .Where(a => a.BusinessId == businessId);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(log => new AuditLogResponseDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityName = log.EntityName,
                    EntityId = log.EntityId,
                    ChangeBy = log.User != null ? log.User.FullName : "System",
                    CreatedAt = log.Timestamp
                })
            .ToListAsync();

        return new PaginatedResponse<AuditLogResponseDto>
        {
            Items = logs,
            TotalCount = totalCount,
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)paginationParams.PageSize)
        };
    }

    public async Task<PaginatedResponse<AdminAuditLogResponseDto>> GetAuditLogsAsync(PaginationParams pagination)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp);

        // // 🔐 ROLE-BASED FILTERING
        // if (requestingUserRole != "super_admin" && requestingUserRole != "admin")
        // {
        //     query = query.Where(a => a.BusinessId == businessId);
        // }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(a => new AdminAuditLogResponseDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                BusinessId = a.BusinessId,
                ChangeBy = a.User != null ? a.User.FullName : "System",
                CreatedAt = a.Timestamp
            })
            .ToListAsync();

        return new PaginatedResponse<AdminAuditLogResponseDto>
        {
            Items = logs,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize)
        };
    }

}