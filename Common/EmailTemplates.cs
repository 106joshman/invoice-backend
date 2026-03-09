using InvoiceService.DTOs;

namespace InvoiceService.Common;
public static class EmailTemplates
{
    public static string BusinessCredentials(BusinessCredentialsEmailDto model)
    {
        return $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <h3>Welcome to Alpha Tech Groups - InvoicePro</h3>

            <p>Hello <strong>{model.FullName}</strong>,</p>

            <p>
                Your business account for
                <strong>{model.BusinessName}</strong> has been created successfully.
            </p>

            <p><a href='{model.Link}'>Click here to set your password</a></p>
            <p>This link expires in 30 minutes.</p>

            <p style='color: #b00020;'>
                Please log in and change your password immediately.
                This password is temporary and will expire.
            </p>

            <p>
                If you did not request this account, please contact support.
            </p>

            <br />

            <p>Regards,<br />
            <strong>Alpha Tech Team</strong></p>
        </div>";
    }

    public static string PasswordReset(BusinessCredentialsEmailDto model)
    {
        return $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
            <h3>Welcome to Alpha Tech Groups - InvoicePro</h3>

            <p>Hello <strong>{model.FullName}</strong>,</p>

            <p>A password reset has been initiated for your account.</p>

            <p> Click this link <a href='{model.Link}'> to reset your password</a></p>
            <p>This link expires in 30 minutes.</p>

            <p>
                If you did not request this account, please contact support.
            </p>

            <br />

            <p>Regards,<br />
            <strong>Alpha Tech Team</strong></p>
        </div>";
    }

    public static string InvoiceNotification(InvoiceEmailDto model)
    {
        return $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6; color:#333; max-width:600px;'>
            <h2 style='color:#2c3e50;'>Invoice from Alpha Tech - InvoicePro</h2>

            <p>Hello <strong>{model.CustomerName}</strong>,</p>

            <p>
                Thank you for doing business with <strong>{model.BusinessName}</strong>.
            </p>

            <p>
                Please find attached the invoice <strong>#{model.InvoiceNumber}</strong>
                for the services/products provided.
            </p>

            <table style='border-collapse: collapse; margin-top:15px;'>
                <tr>
                    <td style='padding:6px 10px;'><strong>Invoice Number:</strong></td>
                    <td style='padding:6px 10px;'>{model.InvoiceNumber}</td>
                </tr>
                <tr>
                    <td style='padding:6px 10px;'><strong>Invoice Date:</strong></td>
                    <td style='padding:6px 10px;'>{model.InvoiceDate}</td>
                </tr>
                <tr>
                    <td style='padding:6px 10px;'><strong>Amount Due:</strong></td>
                    <td style='padding:6px 10px;'>{model.Total}</td>
                </tr>
                <tr>
                    <td style='padding:6px 10px;'><strong>Due Date:</strong></td>
                    <td style='padding:6px 10px;'>{model.DueDate}</td>
                </tr>
            </table>

            <p style='margin-top:20px;'>
                The invoice is attached as a <strong>PDF document</strong>.
                Please review it and make payment before the due date.
            </p>

            <p>
                If you have any questions regarding this invoice, please reply to this email.
            </p>

            <br/>

            <p>
                Best regards,<br/>
                <strong>{model.BusinessName}</strong><br/>
                Alpha Tech Team
            </p>

        </div>";
    }
}
