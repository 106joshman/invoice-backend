namespace InvoiceService.Dtos;

public class AuditLogResponseDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = default!;
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string ChangeBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class AdminAuditLogResponseDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = default!;
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }
    public Guid? BusinessId { get; set; }
    public string ChangeBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
