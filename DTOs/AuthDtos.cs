using System.ComponentModel.DataAnnotations;

namespace InvoiceService.DTOs
{
    public class CreateUserDto
    {
        [Required]
        public required string FullName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public required string Role { get; set; } = "User";
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

    public class UserAuthResponseDto
    {
        public required string Token { get; set; }

        public required string RefreshToken { get; set; }

        public Guid UserId { get; set; }

        public required string FullName { get; set; }

        public required string Role { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public string Message { get; set; } = "User created successfully";
    }
}