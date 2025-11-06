using System.Security.Claims;
using InvoiceService.DTOs;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentController(PaymentService paymentService) : ControllerBase
{
    private readonly PaymentService _paymentService = paymentService;

    [HttpPost("update")]
    public async Task<IActionResult> CreateOrUpdatePayment([FromBody] PaymentInfoRequestDto paymentInfoDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var userId = Guid.Parse(currentUserId);

            var response = await _paymentService.CreateOrUpdatePaymentInfoAsync(userId, paymentInfoDto);

            return Ok(new
            {
                message = "Payment information saved successfully",
                data = response
            });
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
}
