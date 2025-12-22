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

            <p><strong>Email:</strong> {model.ToEmail}</p>
            <p><strong>Temporary Password:</strong> {model.TemporaryPassword}</p>

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
}
