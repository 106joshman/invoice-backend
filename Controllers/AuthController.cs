using System.Security.Claims;
using InvoiceService.DTOs;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService, UserService userService) : ControllerBase
{
    private readonly AuthService _authService = authService;
    private readonly UserService _userService = userService;

    [EnableRateLimiting("registerPolicy")]
    [HttpPost("register-business")]
    [Authorize(Roles = "super_admin,admin")]
    public async Task<IActionResult> RegisterBusiness([FromBody] BusinessRegistrationRequestDto createBusinessDto)
    {
        try
        {
            var superAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(superAdminIdClaim) || !Guid.TryParse(superAdminIdClaim, out Guid superAdminId))
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            var response = await _authService.RegisterBusinessAsync(createBusinessDto, superAdminId);

            return Ok(response);
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

    [EnableRateLimiting("loginPolicy")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        try
        {
            var response = await _authService.Login(loginDto);
            return Ok(response);
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

    [Authorize]
    [HttpPost("invite")]
    public async Task<IActionResult> InviteBusinessUser([FromBody] InviteBusinessUserDto inviteBusinessUserDto)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid("You don't have the jurisdiction to invite this user.");
            }

            var userId = Guid.Parse(currentUserId);

            await _authService.InviteBusinessUserAsync(userId, inviteBusinessUserDto);

            return Ok(new
            {
                message = "User invited successfully",

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

    [Authorize(Roles = "super_admin,admin")]
    [HttpPost("{userId}/resend-credentials")]
    public async Task<IActionResult> ResendUserCredentials(Guid userId)
    {
        try
        {
            var superAdminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(superAdminIdClaim))
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            var adminId = Guid.Parse(superAdminIdClaim);

            await _authService.ResendBusinessCredentialsAsync(
                userId,
                adminId);

            return Ok(new { message = "Credentials resent successfully."});
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
            // Console.WriteLine($"Registration error: {ex.Message}"); // Debugging log
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
    {
        try
        {
            await _authService.SetPassword(dto);

            return Ok(new
            {
                message = "Password set successfully."
            });
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
}