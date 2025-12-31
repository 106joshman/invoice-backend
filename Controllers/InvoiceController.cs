using System.Security.Claims;
using InvoiceService.DTOs;
using InvoiceService.Models;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController(InvoiceServices invoiceService) : ControllerBase
{
    private readonly InvoiceServices _invoiceService = invoiceService;

    [HttpPost("create")]
    public async Task<IActionResult> CreateInvoice([FromBody] InvoiceRequestDto invoiceRequestDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentBusinessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(currentUserId) ||
                string.IsNullOrEmpty(currentBusinessId))
                return Unauthorized(new { message = "Invalid authentication context." });

            var userId = Guid.Parse(currentUserId);
            var businessId = Guid.Parse(currentBusinessId!);

            var response = await _invoiceService.CreateInvoice(businessId, userId, invoiceRequestDto);

            return Ok(new { message = "Invoice created successfully", data = response });
        }
        catch (UnauthorizedAccessException ex)
        {
            // 401 ERROR
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            // 404 ERROR
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("last-number")]
    public async Task<IActionResult> GetLastInvoiceNumber()
    {
        try
        {
            var currentBusinessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(currentBusinessId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var businessId = Guid.Parse(currentBusinessId!);

            var response = await _invoiceService.GetLastInvoiceNumber(businessId);

            return Ok(new
            {
                message = "Last invoice number retrieved successfully",
                data = response
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllInvoice([FromQuery]
        PaginationParams paginationParams,
        string? InvoiceNumber,
        string? CustomerName,
        string? Status)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentBusinessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing user identity." });

            var userId = Guid.Parse(currentUserId);
            var businessId = Guid.Parse(currentBusinessId!);

            var response = await _invoiceService.GetAllInvoice(
                userId,
                businessId,
                paginationParams,
                InvoiceNumber,
                CustomerName,
                Status);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("update/{invoiceId}")]
    public async Task<IActionResult> UpdateInvoice(Guid invoiceId, [FromBody] InvoiceUpdateDto invoiceUpdateDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentBusinessId = User.FindFirstValue("BusinessId");

             if (string.IsNullOrEmpty(currentUserId) ||
                string.IsNullOrEmpty(currentBusinessId))
                return Unauthorized(new { message = "Invalid authentication context." });

            var userId = Guid.Parse(currentUserId);
            var businessId = Guid.Parse(currentBusinessId!);

            var updatedInvoice = await _invoiceService.UpdateInvoice(
                businessId,
                userId,
                invoiceId,
                invoiceUpdateDto);

            return Ok(new
            {
                message = "Invoice updated successfully",
                data = updatedInvoice
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetSingleInvoice(Guid invoiceId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentBusinessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var userId = Guid.Parse(currentUserId);
            var businessId = Guid.Parse(currentBusinessId!);

            var response = await _invoiceService.GetSingleInvoiceAsync(
                businessId,
                userId,
                invoiceId);

            return Ok(new { message = "Invoice retrieved successfully", response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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

    [HttpGet("stats")]
    public async Task<IActionResult> GetInvoiceStats()
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var businessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var userId = Guid.Parse(currentUserId);
            var businessGuid = Guid.Parse(businessId!);

            var response = await _invoiceService.GetInvoiceStatistics(
                businessGuid,
                userId);

            return Ok(new
            {
                message = "Invoice statistics retrieved successfully",
                data = response
            });
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

    [HttpDelete("{invoiceId}")]
    [Authorize]
    public async Task<IActionResult> DeleteInvoice(Guid invoiceId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentBusinessId = User.FindFirstValue("BusinessId");

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var userId = Guid.Parse(currentUserId);
            var businessId = Guid.Parse(currentBusinessId!);

            await _invoiceService.DeleteInvoice(invoiceId, businessId, userId);

            return Ok(new { message = "Invoice deleted successfully." });

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
}