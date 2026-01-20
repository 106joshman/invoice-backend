using InvoiceService.DTOs;
using InvoiceService.Common;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Model;
using Task = System.Threading.Tasks.Task;

namespace InvoiceService.Services;

public enum EmailType
{
    Welcome,
    PasswordReset
}

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly TransactionalEmailsApi _brevo;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;

        var apiKey = _configuration["EmailSettings:ApiKey"]
            ?? throw new Exception("Brevo API key not configured.");

        sib_api_v3_sdk.Client.Configuration.Default.ApiKey.Clear();
        sib_api_v3_sdk.Client.Configuration.Default.ApiKey.Add("api-key", apiKey);

        _brevo = new TransactionalEmailsApi();
    }

    private async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody)
    {
        var fromEmail = _configuration["EmailSettings:FromEmail"];
        var fromName = _configuration["EmailSettings:FromName"];

        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new Exception("Sender email not configured.");

        var email = new SendSmtpEmail
        {
            To = new List<SendSmtpEmailTo>
            {
                new SendSmtpEmailTo(
                    toEmail.ToLowerInvariant(),
                    null
                )
            },
            Sender = new SendSmtpEmailSender
            {
                Email = fromEmail,
                Name = fromName
            },
            Subject = subject,
            HtmlContent = htmlBody
        };

        // Console.WriteLine("📧 Sending email:");
        // Console.WriteLine($"To: {toEmail}");
        // Console.WriteLine($"From: {fromEmail}");
        // Console.WriteLine($"Subject: Set Password");
        // Console.WriteLine($"Body: {htmlBody}");

        await _brevo.SendTransacEmailAsync(email);
    }

    // ===============================
    // PUBLIC METHODS (UNCHANGED API)
    // ===============================

    public async Task SendWelcomeSetPasswordEmailAsync(
        string email,
        string fullName,
        string businessName,
        string link)
    {
        var body = EmailTemplates.BusinessCredentials(
            new BusinessCredentialsEmailDto
            {
                ToEmail = email,
                FullName = fullName,
                BusinessName = businessName,
                Link = link
            }
        );

        await SendEmailAsync(
            email,
            "Welcome to Alpha Tech Groups × InvoicePro",
            body
        );
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string fullName,
        string link)
    {
        var body = EmailTemplates.PasswordReset(
            new BusinessCredentialsEmailDto
            {
                ToEmail = email,
                FullName = fullName,
                Link = link
            }
        );

        await SendEmailAsync(
            email,
            "Reset your password",
            body
        );
    }
}
