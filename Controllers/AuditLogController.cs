using System.Security.Claims;
using InvoiceService.Models;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogController(AuditLogService auditLogService) : ControllerBase
{
    private readonly AuditLogService _auditLogService = auditLogService;

    [HttpGet("business/logs")]
    public async Task<IActionResult> GetBusinessAuditLogs(
        [FromQuery]
        PaginationParams paginationParams)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentBusinessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Invalid or missing user identity." });
            if (string.IsNullOrEmpty(currentBusinessId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var businessId = Guid.Parse(currentBusinessId!);
            var userGuid = Guid.Parse(userId!);

            var logs = await _auditLogService.GetBusinessAuditLogsAsync(
                userGuid,
                businessId,
                paginationParams);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("system")]
    [Authorize(Roles = "super_admin,admin")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery]
        PaginationParams paginationParams)
    {
        try
        {
            var logs = await _auditLogService.GetAuditLogsAsync(paginationParams);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}