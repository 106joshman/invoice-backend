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
public class CustomerController(CustomerService customerService) : ControllerBase
{
    private readonly CustomerService _customerService = customerService;

    [HttpPost("create")]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto customerCreateDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var userId = Guid.Parse(currentUserId);

            var response = await _customerService.CreateCustomer(userId, customerCreateDto);
            return Ok(new { message = "Customer created successfully", data = response });
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{customerId}")]
    public async Task<IActionResult> UpdateCustomer(Guid customerId, [FromBody] CustomerCreateDto customerUpdateDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("User ID not found in token.");

            var userId = Guid.Parse(currentUserId);

            var updatedCustomer = await _customerService.UpdateCustomer(customerId, userId, customerUpdateDto);

            return Ok(new { message = "Customer updated successfully", data = updatedCustomer });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetCustomers([FromQuery]
        PaginationParams paginationParams,
        string? Name,
        string? Company,
        string? Email,
        string? PhoneNumber)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing user identity." });

            var userId = Guid.Parse(currentUserId);

            var response = await _customerService.GetCustomers(
                userId, paginationParams, Name, Company, Email, PhoneNumber);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{customerId}")]
    [Authorize]
    public async Task<IActionResult> DeleteCustomer(Guid customerId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing identity access." });

            var userId = Guid.Parse(currentUserId);

        await _customerService.DeleteCustomer(customerId, userId);

        return Ok(new { message = "Customer deleted successfully." });

        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}