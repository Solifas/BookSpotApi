using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Features.Auth.Commands;
using BookSpot.Application.Features.Auth.Handlers;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public class LoginSecurityTests
{
    [Fact]
    public async Task UnknownAndWrongPassword_UseSameUnauthorizedFailure()
    {
        var jwt = new Mock<IJwtService>();
        var unknownProfiles = new Mock<IProfileRepository>();
        var unknownHandler = new LoginCommandHandler(unknownProfiles.Object, jwt.Object);
        var knownProfiles = new Mock<IProfileRepository>();
        knownProfiles.Setup(repository => repository.GetByEmailAsync("user@example.com")).ReturnsAsync(new Profile
        {
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct horse battery staple")
        });
        var knownHandler = new LoginCommandHandler(knownProfiles.Object, jwt.Object);

        var unknown = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => unknownHandler.Handle(
            new LoginCommand("user@example.com", "wrong password value"), CancellationToken.None));
        var wrong = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => knownHandler.Handle(
            new LoginCommand("user@example.com", "wrong password value"), CancellationToken.None));

        Assert.Equal(unknown.Message, wrong.Message);
    }
}
