namespace InvoiceService.DTOs;

public class BusinessCredentialsEmailDto
{
    public string ToEmail { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string Link { get; set; } = default!;
}

public class EmailSettings
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public bool EnableSSL { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FromName { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
}

public class InvoiceEmailDto
{
    public string ToEmail { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string InvoiceNumber { get; set; } = default!;
    public string InvoiceDate { get; set; } = default!;
    public string DueDate { get; set; } = default!;
    public string Total { get; set; } = default!;
}

public class AdminNotificationEmailDto
{
    public string Name { get; set; } = default!;
    public string IndustryGroup { get; set; } = default!;
    public string IndustrySector { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string SubscriptionPlan { get; set; } = default!;
}