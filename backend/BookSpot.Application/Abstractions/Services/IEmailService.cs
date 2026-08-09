namespace BookSpot.Application.Abstractions.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendPasswordResetConfirmationAsync(string email);
}