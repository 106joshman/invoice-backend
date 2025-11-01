namespace InvoiceService.Models;

public class PaymentInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;

    // Relationship
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
}
