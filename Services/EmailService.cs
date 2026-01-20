// using InvoiceService.DTOs;
// using System.Net.Mail;
// using InvoiceService.Common;

// namespace InvoiceService.Services;

// public enum EmailType
// {
//     welcome,
//     PasswordReset
// }

// public class EmailService(IConfiguration configuration)
// {
//     private readonly IConfiguration _configuration = configuration;

//     private SmtpClient CreateSmtp()
//     {
//         var host = _configuration["EmailSettings:Host"] ?? "smtp-relay.brevo.com";
//         var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");

//         return new SmtpClient(host, port)
//         {
//             EnableSsl = true,
//             UseDefaultCredentials = false,
//             Credentials = new System.Net.NetworkCredential(
//                 _configuration["EmailSettings:Username"],
//                 _configuration["EmailSettings:Password"]
//             ),
//             DeliveryMethod = SmtpDeliveryMethod.Network,
//             Timeout = 20000
//         };
//     }

//     public async Task SendEmailAsync(
//         string toEmail,
//         string subject,
//         string htmlBody)
//     {
//         using var smtp = CreateSmtp();

//         var fromEmail = _configuration["EmailSettings:FromEmail"];

//         if (string.IsNullOrWhiteSpace(fromEmail))
//         {
//             throw new Exception("Email configuration is missing or invalid.");
//         }

//         var message = new MailMessage
//         {
//             From = new MailAddress(
//                 fromEmail,
//                 _configuration["EmailSettings:FromName"]
//             ),
//             Subject = subject,
//             Body = htmlBody,
//             IsBodyHtml = true
//         };

//         message.To.Add(toEmail.ToLowerInvariant());
//         Console.WriteLine("📧 Sending email:");
//         Console.WriteLine($"To: {toEmail}");
//         Console.WriteLine($"From: {fromEmail}");
//         Console.WriteLine($"Subject: Set Password");
//         Console.WriteLine($"Body: {htmlBody}");


//         await smtp.SendMailAsync(message);
//     }

//     public async Task SendWelcomeSetPasswordEmailAsync(
//         string email,
//         string fullName,
//         string businessName,
//         string link)
//     {

//         var body = EmailTemplates.BusinessCredentials(
//             new BusinessCredentialsEmailDto
//             {
//                 ToEmail = email,
//                 FullName = fullName,
//                 BusinessName = businessName,
//                 Link = link
//             }
//         );

//         await SendEmailAsync(
//             email,
//             "Welcome to Alpha Tech Groups X InvoicePro",
//             body
//         );
//     }

//     public async Task SendPAsswordResetEmailAsync(
//         string email,
//         string link
//     )
//     {
//         var body = EmailTemplates.PasswordReset(
//             new BusinessCredentialsEmailDto
//             {
//                 ToEmail = email,
//                 Link = link
//             }
//         );

//         await SendEmailAsync(
//             email,
//             "Reset your password",
//             body
//         );
//     }
// }


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

        var apiKey = _configuration["EmailSettings:BrevoApiKey"]
            ?? throw new Exception("Brevo API key not configured.");

        sib_api_v3_sdk.Client.Configuration.Default.ApiKey["api-key"] = apiKey;

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
                new() { Email = toEmail.ToLowerInvariant() }
            },
            Sender = new SendSmtpEmailSender
            {
                Email = fromEmail,
                Name = fromName
            },
            Subject = subject,
            HtmlContent = htmlBody
        };

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
