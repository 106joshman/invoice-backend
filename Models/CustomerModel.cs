using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Models;

[Index(nameof(Email), IsUnique = true)]
public class Customer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public required string Name { get; set; }
    [Required, EmailAddress]
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // RELATIOSHIP
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // NAVIGATION PROPERTY
    public ICollection<Invoice> Invoices { get; set; } = [];
}