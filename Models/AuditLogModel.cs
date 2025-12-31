namespace InvoiceService.Models;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Action { get; set; } = string.Empty; // e.g., "CREATE", "UPDATE", "DELETE"
    public required string EntityName { get; set; } = string.Empty; // e.g., "INVOICE", "CUSTOMER"
    public required Guid EntityId { get; set; }
    public string? ChangeBy { get; set; } // USER EMAIL OR ID
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // RELATIONSHIP
    public Guid UserId { get; set; }
    public Guid? BusinessId { get; set; }
    public User? User { get; set; }
}