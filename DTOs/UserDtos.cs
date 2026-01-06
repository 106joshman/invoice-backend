using System.ComponentModel.DataAnnotations;

namespace InvoiceService.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class SetPasswordDto
{
    public Guid UserId { get; set; }
    public required string Token { get; set; } = default!;
    public required string NewPassword { get; set; } = default!;
}

public class UserUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyLogo { get; set; } = string.Empty;
}