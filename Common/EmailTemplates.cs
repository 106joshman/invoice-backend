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

    public static string AdminNotification(AdminNotificationEmailDto model)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
            <title>New Business Registration</title>
            </head>
            <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,sans-serif;">

            <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f4;padding:40px 0;">
                <tr>
                <td align="center">

                    <!-- Email card -->
                    <table width="600" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e0e0e0;">

                    <!-- Header -->
                    <tr>
                        <td style="background-color:#1a1a2e;padding:28px 32px;">
                        <p style="margin:0;font-size:11px;color:#a0a0b0;letter-spacing:1.5px;text-transform:uppercase;">Admin Notification</p>
                        <h1 style="margin:6px 0 0;font-size:20px;color:#ffffff;font-weight:600;">New Business Registration</h1>
                        </td>
                    </tr>

                    <!-- Intro -->
                    <tr>
                        <td style="padding:28px 32px 8px;">
                        <p style="margin:0;font-size:14px;color:#555555;line-height:1.6;">
                            A new business has registered and is <strong style="color:#1a1a2e;">pending review</strong>.
                            Please log in to the admin dashboard to verify this account.
                        </p>
                        </td>
                    </tr>

                    <!-- Details table -->
                    <tr>
                        <td style="padding:16px 32px 28px;">
                        <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e8e8e8;border-radius:6px;overflow:hidden;">

                            <tr style="background-color:#f9f9f9;">
                            <td style="padding:12px 16px;font-size:12px;font-weight:600;color:#888888;text-transform:uppercase;letter-spacing:0.8px;width:35%;border-bottom:1px solid #e8e8e8;">Business</td>
                            <td style="padding:12px 16px;font-size:14px;color:#1a1a2e;font-weight:600;border-bottom:1px solid #e8e8e8;">{model.Name}</td>
                            </tr>

                            <tr>
                            <td style="padding:12px 16px;font-size:12px;font-weight:600;color:#888888;text-transform:uppercase;letter-spacing:0.8px;border-bottom:1px solid #e8e8e8;">Industry</td>
                            <td style="padding:12px 16px;font-size:14px;color:#333333;border-bottom:1px solid #e8e8e8;">{model.IndustryGroup} / {model.IndustrySector}</td>
                            </tr>

                            <tr style="background-color:#f9f9f9;">
                            <td style="padding:12px 16px;font-size:12px;font-weight:600;color:#888888;text-transform:uppercase;letter-spacing:0.8px;border-bottom:1px solid #e8e8e8;">Owner</td>
                            <td style="padding:12px 16px;font-size:14px;color:#333333;border-bottom:1px solid #e8e8e8;">{model.FullName} &nbsp;<span style="color:#888888;">({model.Email})</span></td>
                            </tr>

                            <tr>
                            <td style="padding:12px 16px;font-size:12px;font-weight:600;color:#888888;text-transform:uppercase;letter-spacing:0.8px;">Plan</td>
                            <td style="padding:12px 16px;">
                                <span style="display:inline-block;background-color:#e8f4fd;color:#1565c0;font-size:12px;font-weight:600;padding:4px 10px;border-radius:4px;">{model.SubscriptionPlan}</span>
                            </td>
                            </tr>

                        </table>
                        </td>
                    </tr>

                    <!-- CTA Button -->
                    <tr>
                        <td align="center" style="padding:0 32px 36px;">
                        <a href="https://invoice-booking.vercel.app" style="display:inline-block;background-color:#1a1a2e;color:#ffffff;text-decoration:none;font-size:14px;font-weight:600;padding:12px 28px;border-radius:6px;letter-spacing:0.5px;">
                            Review in Admin Dashboard
                        </a>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color:#f9f9f9;padding:16px 32px;border-top:1px solid #e8e8e8;">
                        <p style="margin:0;font-size:12px;color:#aaaaaa;text-align:center;">
                            This is an automated notification. Please do not reply to this email.
                        </p>
                        </td>
                    </tr>

                    </table>
                </td>
                </tr>
            </table>

            </body>
            </html>
            """;
    }
}
