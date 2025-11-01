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
        public required string Role { get; set; } = "User";

        // Optional — for profile update later
        public string Address { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;

        // Subscription control
        public string SubscriptionPlan { get; set; } = "Free"; // Free, Pro, etc.
        public int MonthlyInvoiceCount { get; set; } = 0;
        public PaymentInfo? PaymentInfo { get; set; }

        // Navigation
        public ICollection<Customer>? Customers { get; set; }
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}