using System.Security.Claims;
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
    public async Task<ActionResult> GetUserProfile(Guid userId)
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

    // UPDATE USER PROFLE
    [HttpPut("update-profile")]
    [Authorize]
    public async Task<ActionResult> UpdateProfile([FromBody] UserUpdateDto updateDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role) ?? "User";

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "Invalid or missing user identity." });

            var userGuid = Guid.Parse(currentUserId);

            var updatedProfile = await _userService.UpdateUserAsync(userGuid, updateDto);

            return Ok(new
                {
                    message = "Profile updated successfully",
                    data = updatedProfile
                });
        }
        catch (UnauthorizedAccessException ex)
        {
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

    [HttpPost("change-password/{userId}")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(Guid userId, [FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            // VERIFY USER CHANGING PASSWORD
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != userId.ToString())
            {
                return Forbid("CALL THE POLICE NOW!!! You cannot change password for another user.");
            }

            await _userService.ChangePassword(userId, changePasswordDto);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
        //    Console.WriteLine($"Registration error: {ex.Message}");
            return BadRequest(ex.Message);
        }
    }
}