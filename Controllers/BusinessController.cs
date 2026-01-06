using System.Security.Claims;
using InvoiceService.Models;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace invoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BusinessController(BusinessService businessService, EmailService _emailService) : ControllerBase
{
    private readonly BusinessService _businessService = businessService;
    private readonly EmailService _emailService = _emailService;

    [HttpGet("{businessId}")]
    public async Task<ActionResult> GetBusinessById(Guid businessId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "User";

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing user identity." });

            var userGuid = Guid.Parse(currentUserId);

            var business = await _businessService.GetBusinessByIdAsync(
                businessId,
                userGuid,
                currentUserRole);

            return Ok(business);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [Authorize(Roles = "Admin,super_admin")]
    [HttpGet("all-businesses")]
    public async Task<IActionResult> GetAllBusinesses([FromQuery] PaginationParams paginationParams,
    string? Name,
    string? SubscriptionPlan)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "User";

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing user identity." });

            var userGuid = Guid.Parse(currentUserId);

            var businesses = await _businessService.GetAllBusinessAsync(
                requestingUserId: userGuid,
                requestingUserRole: currentUserRole,
                paginationParams: paginationParams,
                Name: Name,
                SubscriptionPlan: SubscriptionPlan);

            return Ok(businesses);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("test-email")]
    public async Task<IActionResult> TestEmail()
    {
        await _emailService.SendWelcomeSetPasswordEmailAsync(
            "ejembijoshman@gmail.com",
            "Test User",
            "Test Business",
            "Temp123!"
        );

        return Ok("Email sent");
    }


    // public async Task<ActionResult> UpdateBusiness(Guid businessId, [FromBody] BusinessUpdateDto updateDto)
    // {
    //     try
    //     {
    //         var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    //         var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "User";

    //         if (string.IsNullOrEmpty(currentUserId))
    //             return Unauthorized(new { message = "Invalid or missing user identity." });

    //         var userGuid = Guid.Parse(currentUserId);

    //         var updatedBusiness = await _businessService.UpdateBusinessAsync(
    //             businessId,
    //             userGuid,
    //             currentUserRole,
    //             updateDto);

    //         return Ok(new
    //         {
    //             message = "Business updated successfully",
    //             data = updatedBusiness
    //         });
    //     }
    //     catch (UnauthorizedAccessException ex)
    //     {
    //         return Unauthorized(new { message = ex.Message });
    //     }
    //     catch (KeyNotFoundException ex)
    //     {
    //         return NotFound(new { message = ex.Message });
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(new { message = ex.Message });
    //     }
    // }
}