using System.ComponentModel.DataAnnotations;

namespace InvoiceService.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string Address { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyLogo { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = "Free";
    public int MonthlyInvoiceCount { get; set; }
    public PaymentInfoResponseDto? PaymentInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

public class ForceChangePasswordDto
{
    public required string NewPassword { get; set; }
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