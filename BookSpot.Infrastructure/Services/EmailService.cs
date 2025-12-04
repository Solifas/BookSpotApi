using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using BookSpot.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookSpot.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public EmailService(
        IAmazonSimpleEmailService sesClient,
        ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _sesClient = sesClient;
        _logger = logger;
        _configuration = configuration;
        _senderEmail = configuration["Email:SenderEmail"] ?? "noreply@bookspot.com";
        _senderName = configuration["Email:SenderName"] ?? "BookSpot";
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        try
        {
            var subject = "Reset Your Password - BookSpot";
            var htmlBody = GeneratePasswordResetEmailHtml(resetLink);
            var textBody = GeneratePasswordResetEmailText(resetLink);

            await SendEmailAsync(email, subject, htmlBody, textBody);

            _logger.LogInformation("Password reset email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            throw;
        }
    }

    public async Task SendPasswordResetConfirmationAsync(string email)
    {
        try
        {
            var subject = "Password Reset Successful - BookSpot";
            var htmlBody = GeneratePasswordResetConfirmationHtml();
            var textBody = GeneratePasswordResetConfirmationText();

            await SendEmailAsync(email, subject, htmlBody, textBody);

            _logger.LogInformation("Password reset confirmation email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset confirmation email to {Email}", email);
            throw;
        }
    }

    private async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, string textBody)
    {
        var sendRequest = new SendEmailRequest
        {
            Source = $"{_senderName} <{_senderEmail}>",
            Destination = new Destination
            {
                ToAddresses = new List<string> { recipientEmail }
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = htmlBody
                    },
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = textBody
                    }
                }
            }
        };

        await _sesClient.SendEmailAsync(sendRequest);
    }

    private string GeneratePasswordResetEmailHtml(string resetLink)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Reset Your Password</title>
                </head>
                <body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
                    <div style=""background-color: #f8f9fa; padding: 20px; border-radius: 10px;"">
                        <h1 style=""color: #2c3e50; margin-bottom: 20px;"">Reset Your Password</h1>
                        
                        <p>Hello,</p>
                        
                        <p>We received a request to reset your password for your BookSpot account. Click the button below to reset your password:</p>
                        
                        <div style=""text-align: center; margin: 30px 0;"">
                            <a href=""{resetLink}"" 
                            style=""background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;"">
                                Reset Password
                            </a>
                        </div>
                        
                        <p>Or copy and paste this link into your browser:</p>
                        <p style=""background-color: #e9ecef; padding: 10px; border-radius: 5px; word-break: break-all;"">
                            {resetLink}
                        </p>
                        
                        <p style=""color: #dc3545; font-weight: bold; margin-top: 20px;"">
                            ⚠️ This link will expire in 1 hour.
                        </p>
                        
                        <p>If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>
                        
                        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;"">
                        
                        <p style=""font-size: 12px; color: #6c757d;"">
                            This is an automated email from BookSpot. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";
    }

    private string GeneratePasswordResetEmailText(string resetLink)
    {
        return $@"Reset Your Password

                Hello,

                We received a request to reset your password for your BookSpot account.

                To reset your password, click the link below or copy and paste it into your browser:

                {resetLink}

                ⚠️ This link will expire in 1 hour.

                If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.

                ---
                This is an automated email from BookSpot. Please do not reply to this email.";
    }

    private string GeneratePasswordResetConfirmationHtml()
    {
        return @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>Password Reset Successful</title>
                </head>
                <body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
                    <div style=""background-color: #f8f9fa; padding: 20px; border-radius: 10px;"">
                        <h1 style=""color: #28a745; margin-bottom: 20px;"">✓ Password Reset Successful</h1>
                        
                        <p>Hello,</p>
                        
                        <p>Your password has been successfully reset. You can now log in to your BookSpot account with your new password.</p>
                        
                        <p style=""background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0;"">
                            <strong>Security Tip:</strong> If you didn't make this change, please contact our support team immediately.
                        </p>
                        
                        <p>For your security, we recommend:</p>
                        <ul>
                            <li>Using a strong, unique password</li>
                            <li>Enabling two-factor authentication (if available)</li>
                            <li>Never sharing your password with anyone</li>
                        </ul>
                        
                        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;"">
                        
                        <p style=""font-size: 12px; color: #6c757d;"">
                            This is an automated email from BookSpot. Please do not reply to this email.
                        </p>
                    </div>
                </body>
                </html>";
    }

    private string GeneratePasswordResetConfirmationText()
    {
        return @"Password Reset Successful

                Hello,

                Your password has been successfully reset. You can now log in to your BookSpot account with your new password.

                ⚠️ Security Tip: If you didn't make this change, please contact our support team immediately.

                For your security, we recommend:
                - Using a strong, unique password
                - Enabling two-factor authentication (if available)
                - Never sharing your password with anyone

                ---
                This is an automated email from BookSpot. Please do not reply to this email.";
    }
}
