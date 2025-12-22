namespace InvoiceService.DTOs;

public class BusinessResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
     public string SubscriptionPlan { get; set; } = default!;
    public bool IsMultiTenant { get; set; }
    public string BrandColor { get; set; } = default!;
    public string CompanyLogoUrl { get; set; } = default!;
    public List<BBusinessMemberDto> TeamMembers { get; set; } = [];
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