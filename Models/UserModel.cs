using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string FullName { get; set; }
        [EmailAddress]
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;

        // 🔐 SYSTEM-WIDE  ROLE
        public required string Role { get; set; } = "User";
        // User | Admin | SuperAdmin

        public bool IsDeleted { get; set; } = false;

        // NAVIGATION
        public BusinessUser? BusinessUsers { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
