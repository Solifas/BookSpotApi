using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Features.Auth;
using BookSpot.Application.Features.Auth.Commands;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public class PasswordResetIntegrityTests
{
    [Fact]
    public async Task Reset_UsesSingleAtomicConsumeBoundary()
    {
        var rawToken = ResetTokenRules.Generate();
        var token = new PasswordResetToken
        {
            Token = ResetTokenRules.Digest(rawToken),
            UserId = "profile-1",
            Email = "user@example.com",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        var tokens = new Mock<IPasswordResetTokenRepository>();
        tokens.Setup(repository => repository.GetAsync(token.Token)).ReturnsAsync(token);
        tokens.Setup(repository => repository.TryConsumeAsync(token.Token, "profile-1", It.IsAny<string>(), 1))
            .ReturnsAsync(true);
        var profiles = new Mock<IProfileRepository>();
        profiles.Setup(repository => repository.GetAsync("profile-1")).ReturnsAsync(new Profile
        {
            Id = "profile-1",
            Email = "user@example.com",
            SecurityVersion = 1
        });
        var email = new Mock<IEmailService>();
        var handler = new ResetPasswordHandler(profiles.Object, tokens.Object, email.Object);

        Assert.True(await handler.Handle(
            new ResetPasswordCommand(rawToken, "correct horse battery staple"),
            CancellationToken.None));

        tokens.Verify(repository => repository.TryConsumeAsync(token.Token, "profile-1", It.IsAny<string>(), 1), Times.Once);
        profiles.Verify(repository => repository.SaveAsync(It.IsAny<Profile>()), Times.Never);
        tokens.Verify(repository => repository.SaveAsync(It.IsAny<PasswordResetToken>()), Times.Never);
    }
}
