
namespace InvoiceService.DTOs;

public class InvoiceItemRequestDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceItemResponseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceItemUpdateDto
{
    public Guid? Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceRequestDto
{
    public Guid CustomerId { get; set; }
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
    public List<InvoiceItemRequestDto> Items { get; set; } = [];
}

public class InvoiceResponseDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public CustomerResponseDto Customer { get; set; } = new();
    public List<InvoiceItemResponseDto> Items { get; set; } = new();
}

public class LastInvoiceNumberResponseDto
{
    public string? LastInvoiceNumber { get; set; }
}

public class InvoiceUpdateDto
{
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? Discount { get; set; }
    public decimal? Total { get; set; }
    public List<InvoiceItemUpdateDto>? Items { get; set; } = [];
}
