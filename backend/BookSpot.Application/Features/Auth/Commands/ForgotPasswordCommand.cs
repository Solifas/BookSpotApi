using System.Security.Cryptography;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace BookSpot.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<bool>;

public class ForgotPasswordHandler(
    IProfileRepository profiles,
    IPasswordResetTokenRepository resetTokens,
    IEmailService emailService,
    IConfiguration configuration)
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
        var resetToken = GenerateSecureToken();

        // Create reset token entity
        var passwordResetToken = new PasswordResetToken
        {
            Token = resetToken,
            Email = request.Email,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
            CreatedAt = DateTime.UtcNow
        };

        // Save reset token
        await resetTokens.SaveAsync(passwordResetToken);

        // Generate reset link
        var baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

        // Send email
        await emailService.SendPasswordResetEmailAsync(request.Email, resetLink);

        return true;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
