using System.Security.Claims;
using InvoiceService.DTOs;
using InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    private readonly AuthService _authService = authService;

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

    [HttpPost("resend-credentials")]
    public async Task<IActionResult> ResendCredentials(Guid userId)
    {
        try
        {
            var superAdminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(superAdminIdClaim) || !Guid.TryParse(superAdminIdClaim, out Guid superAdminId))
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            await _authService.ResendBusinessCredentialsAsync(userId);
            return Ok("Credentials resent successfully.");
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

    [HttpPost("force-change-password")]
    public async Task<IActionResult> ForceChangePassword([FromBody] string newTempPassword)
    {
        try
        {
            // VERIFY USER CHANGING PASSWORD
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Forbid("Incorrect credentials.");
            }

            var userId = Guid.Parse(currentUserId);

            await _authService.ForceChangePassword(userId, newTempPassword);
            return Ok(new { message = "New Password generated successfully." });
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