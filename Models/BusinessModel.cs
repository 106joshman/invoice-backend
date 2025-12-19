using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InvoiceService.Models;

 [Index(nameof(Name), IsUnique = true)]
public class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsMultiUserEnabled { get; set; } = false;
    public string SubscriptionPlan { get; set; } = "Free"; // FREE, PRO, etc.
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string CompanyLogoUrl { get; set; } = string.Empty;
    public string BrandColor { get; set; } = "#000000";
    public int MonthlyInvoiceCount { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;
    public DateTime? LastInvoiceReset { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<BusinessUser> BusinessUsers { get; set; } = [];
    public PaymentInfo? PaymentInfo { get; set; }
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Index(nameof(UserId), IsUnique = true)]
public class BusinessUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = default!;

    // BUSINESS CONTEXT ROLE
    public string Role { get; set; } = "Owner";
    // OWNER | ADMIN | MEMBER

    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public bool IsActive { get; set; } = true;
    // ACCOUNT STATUS IN BUSINESS CONTEXT
    // TRUE = ACTIVE
    // FALSE = SUSPENDED
    public bool IsVerified { get; set; } = false;
    // EMAIL VERIFIED FOR BUSINESS CONTEXT
    // TRUE = LOGGED IN AT LEAST ONCE
    // FALSE = INVITED BUT NEVER LOGGED IN SETS TO TRUE ON FIRST LOGIN
    public bool IsDeleted { get; set; } = false;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}