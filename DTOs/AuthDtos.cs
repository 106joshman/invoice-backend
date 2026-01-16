using System.ComponentModel.DataAnnotations;

namespace InvoiceService.DTOs
{
    public class BusinessRegistrationRequestDto
    {
        [Required]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [EmailAddress]
        public required string BusinessEmail { get; set; }

        // BUSINESS
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsMultiTenant { get; set; }
        public string? Message { get; set; }
    }

    public class BusinessRegistrationResponseDto
    {
        public Guid BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public string? BusinessEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsMultiTenant { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
    }

    public class InviteBusinessUserDto
    {
        public Guid BusinessId { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string BusinessRole { get; set; } = "Member"; // Admin | Member
        public string? Message { get; set; }
    }

    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public Guid UserId { get; set; }
        public Guid? BusinessId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public string? BusinessRole { get; set; }
        public bool? IsVerified { get; set; }
        public bool? IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Message { get; set; }
        public bool? RequirePasswordChange { get; set; }
    }
}