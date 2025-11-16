using BookSpot.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace BookSpot.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        // TODO: Implement actual email sending (AWS SES, SendGrid, etc.)
        // For now, just log the email content
        _logger.LogInformation("Password reset email would be sent to: {Email}", email);
        _logger.LogInformation("Reset link: {ResetLink}", resetLink);

        // Simulate async email sending
        await Task.Delay(100);

        _logger.LogInformation("Password reset email sent successfully to {Email}", email);
    }

    public async Task SendPasswordResetConfirmationAsync(string email)
    {
        // TODO: Implement actual email sending
        _logger.LogInformation("Password reset confirmation email would be sent to: {Email}", email);

        // Simulate async email sending
        await Task.Delay(100);

        _logger.LogInformation("Password reset confirmation email sent successfully to {Email}", email);
    }
}