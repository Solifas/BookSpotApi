using BookSpot.Domain.Entities;

namespace BookSpot.Application.Abstractions.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetAsync(string token);
    Task<bool> TryConsumeAsync(string token, string profileId, string passwordHash, int expectedSecurityVersion);
    Task SaveAsync(PasswordResetToken resetToken);
    Task DeleteAsync(string token);
    Task<IEnumerable<PasswordResetToken>> GetByEmailAsync(string email);
}