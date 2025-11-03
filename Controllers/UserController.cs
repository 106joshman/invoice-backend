using InvoiceService.DTOs;
using InvoiceService.Models;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserService userService) : ControllerBase
{
    private readonly UserService _userService = userService;

    [HttpGet("profile/{userId}")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> GetUserProfile(Guid userId)
    {
        try
        {
            var response = await _userService.GetUserProfile(userId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,super_admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams paginationParams,
        string? FullName,
        string? BusinessName,
        string? Email)
    {
        try
        {
            var response = await _userService.GetAllUsers(paginationParams, FullName, BusinessName, Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while fetching users." });
        }
    }

    [Authorize(Roles = "Admin,super_admin")]
    [HttpGet("admins")]
    public async Task<IActionResult> GetAllAdmins([FromQuery] PaginationParams paginationParams,
    string? FullName,
    string? Email)
    {
        try
        {
            var response = await _userService.GetAdminsAndSuperAdmins(paginationParams, FullName, Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while fetching admins." });
        }
    }
}