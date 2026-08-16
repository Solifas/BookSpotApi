using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Features.Auth.Commands;
using BookSpot.Application.Features.Auth.Handlers;
using BookSpot.Domain.Entities;
using Moq;

namespace BookSpot.Tests;

public class RegistrationIntegrityTests
{
    [Fact]
    public async Task Register_UsesAtomicNormalizedEmailClaim()
    {
        var profiles = new Mock<IProfileRepository>();
        Profile? saved = null;
        profiles.Setup(repository => repository.CreateAsync(It.IsAny<Profile>()))
            .Callback<Profile>(profile => saved = profile)
            .ReturnsAsync(true);
        var jwt = new Mock<IJwtService>();
        jwt.Setup(service => service.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("jwt");
        var handler = new RegisterCommandHandler(profiles.Object, jwt.Object);

        var result = await handler.Handle(
            new RegisterCommand("  User@Example.COM  ", "User", null, "correct horse battery staple", "client"),
            CancellationToken.None);

        profiles.Verify(repository => repository.CreateAsync(It.IsAny<Profile>()), Times.Once);
        profiles.Verify(repository => repository.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        profiles.Verify(repository => repository.SaveAsync(It.IsAny<Profile>()), Times.Never);
        Assert.Equal("user@example.com", saved!.EmailNormalized);
        Assert.Equal("user@example.com", saved.Email);
        Assert.Equal(saved.Id, result.Profile.ProfileId);
    }
}
