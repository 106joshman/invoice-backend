using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Models;

[Index(nameof(InvoiceNumber), IsUnique = true)]
public class Invoice
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string InvoiceNumber { get; set; }
    public required string Status { get; set; } = "draft";
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Relationships
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    [ForeignKey(nameof(Customer))]
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}