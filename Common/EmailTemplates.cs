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
}
