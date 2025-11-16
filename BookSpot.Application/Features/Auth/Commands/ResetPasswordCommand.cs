using System.Security.Cryptography;
using System.Text;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.Auth.Commands;

public record ResetPasswordCommand(
    string Token,
    string NewPassword
) : IRequest<bool>;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IProfileRepository _profiles;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly IEmailService _emailService;

    public ResetPasswordHandler(
        IProfileRepository profiles,
        IPasswordResetTokenRepository resetTokens,
        IEmailService emailService)
    {
        _profiles = profiles;
        _resetTokens = resetTokens;
        _emailService = emailService;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate token
        var resetToken = await _resetTokens.GetAsync(request.Token);
        if (resetToken == null)
        {
            throw new ValidationException("Invalid or expired reset token.");
        }

        // Check if token is expired
        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            await _resetTokens.DeleteAsync(request.Token);
            throw new ValidationException("Reset token has expired. Please request a new password reset.");
        }

        // Check if token has already been used
        if (resetToken.IsUsed)
        {
            throw new ValidationException("Reset token has already been used. Please request a new password reset.");
        }

        // Get user
        var user = await _profiles.GetAsync(resetToken.UserId);
        if (user == null)
        {
            throw new ValidationException("User not found.");
        }

        // Validate new password
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new ValidationException("Password must be at least 6 characters long.");
        }

        // Hash new password
        var hashedPassword = HashPassword(request.NewPassword);

        // Update user password
        user.PasswordHash = hashedPassword;
        await _profiles.SaveAsync(user);

        // Mark token as used
        resetToken.IsUsed = true;
        await _resetTokens.SaveAsync(resetToken);

        // Send confirmation email
        await _emailService.SendPasswordResetConfirmationAsync(user.Email);

        return true;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}