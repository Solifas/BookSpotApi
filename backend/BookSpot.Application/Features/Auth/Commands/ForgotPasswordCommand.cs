using System.Security.Cryptography;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<bool>;

public class ForgotPasswordHandler(
    IProfileRepository profiles,
    IPasswordResetTokenRepository resetTokens,
    IEmailService emailService)
    : IRequestHandler<ForgotPasswordCommand, bool>
{
    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Check if user exists
        var user = await profiles.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // Don't reveal that the email doesn't exist for security reasons
            // But still return true to prevent email enumeration attacks
            return true;
        }

        // Invalidate any existing reset tokens for this email
        var existingTokens = await resetTokens.GetByEmailAsync(request.Email);
        foreach (var token in existingTokens)
        {
            await resetTokens.DeleteAsync(token.Token);
        }

        // Generate secure reset token
        var resetToken = ResetTokenRules.Generate();

        // Create reset token entity
        var passwordResetToken = new PasswordResetToken
        {
            Token = ResetTokenRules.Digest(resetToken),
            Email = AuthRules.NormalizeEmail(request.Email),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };

        // Save reset token
        await resetTokens.SaveAsync(passwordResetToken);

        // Generate reset link
        var baseUrl = Environment.GetEnvironmentVariable("BOOKSPOT_PUBLIC_BASE_URL") ?? "http://localhost:8080";
        var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password#{resetToken}";

        // Send email
        await emailService.SendPasswordResetEmailAsync(request.Email, resetLink);

        return true;
    }

}
