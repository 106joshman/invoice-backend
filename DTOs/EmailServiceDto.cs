namespace InvoiceService.DTOs;

public class BusinessCredentialsEmailDto
{
    public string ToEmail { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string TemporaryPassword { get; set; } = default!;
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
