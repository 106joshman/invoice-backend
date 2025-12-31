namespace InvoiceService.DTOs;

public class BusinessResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
     public string SubscriptionPlan { get; set; } = default!;
    public int MonthlyInvoiceCount { get; set; }
    public bool IsMultiTenant { get; set; }
    public string BrandColor { get; set; } = default!;
    public string CompanyLogoUrl { get; set; } = default!;
    public List<BBusinessMemberDto> TeamMembers { get; set; } = [];
    public PaymentInfoResponseDto? PaymentInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BBusinessMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class BusinessInvoiceStatsDto
{
    public Guid BusinessId { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOverdue { get; set; }
    public int DraftCount { get; set; }
}
