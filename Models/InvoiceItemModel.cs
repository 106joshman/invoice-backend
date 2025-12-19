using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceService.Models
{
    public class InvoiceItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        [Required]
        public decimal Amount { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Relationship
        [ForeignKey(nameof(Invoice))]
        public Guid InvoiceId { get; set; }
        public Invoice? Invoice { get; set; } = default!;
    }
}
