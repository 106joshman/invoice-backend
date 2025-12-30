using InvoiceService.DTOs;
using System.Net.Mail;
using InvoiceService.Common;

namespace InvoiceService.Services;

public class EmailService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string fullName,
        string businessName,
        string temporaryPassword)
    {
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];
        var fromEmail = _configuration["EmailSettings:FromEmail"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new Exception("Email configuration is missing or invalid.");
        }

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new System.Net.NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, _configuration["EmailSettings:FromName"]),
            Subject = "Welcome to Alpha Tech Groups X InvoicePro",
            Body = EmailTemplates.BusinessCredentials(new BusinessCredentialsEmailDto
            {
                ToEmail = toEmail,
                FullName = fullName,
                BusinessName = businessName,
                TemporaryPassword = temporaryPassword
            }),
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        await smtp.SendMailAsync(message);
    }
}