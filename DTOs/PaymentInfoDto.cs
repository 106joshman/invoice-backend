namespace InvoiceService.DTOs;

public class PaymentInfoRequestDto
{
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
}

public class PaymentInfoResponseDto
{
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
}