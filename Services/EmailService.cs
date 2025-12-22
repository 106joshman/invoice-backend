using Microsoft.Extensions.Options;
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
        var smtp = new SmtpClient
        {
            Host = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com",
            Port = int.Parse(_configuration["EmailSettings:Port"] ?? "587"),
            EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSSL"] ?? "true"),
            Credentials = new System.Net.NetworkCredential(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        var message = new MailMessage
        {
            From = new MailAddress(
                _configuration["EmailSettings:FromEmail"] ?? "",
                _configuration["EmailSettings:FromName"]),
            Subject = "Welcome to Alpha Tech Groups X InvoicePro",
            Body = EmailTemplates.BusinessCredentials(new
            BusinessCredentialsEmailDto
            {
                ToEmail = toEmail,
                FullName = fullName,
                BusinessName = businessName,
                TemporaryPassword = temporaryPassword
            }),

            IsBodyHtml = true,
        };

        message.To.Add(toEmail);
        await smtp.SendMailAsync(message);
    }
}