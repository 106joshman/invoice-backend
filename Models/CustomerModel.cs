using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Models;

[Index(nameof(Email), nameof(BusinessId), IsUnique = true)]
public class Customer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public required string Name { get; set; }
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;

    // RELATIOSHIP
    [ForeignKey(nameof(Business))]
    public Guid BusinessId { get; set; }
    public Business? Business { get; set; }

    // NAVIGATION PROPERTY
    public ICollection<Invoice> Invoices { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}