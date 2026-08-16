using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Auth;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Auth.Commands;
using MediatR;
using BCrypt.Net;

namespace BookSpot.Application.Features.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IProfileRepository profileRepository, IJwtService jwtService)
    {
        _profileRepository = profileRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByEmailAsync(AuthRules.NormalizeEmail(request.Email));

        if (profile == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, profile.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(profile.Id, profile.Email, profile.UserType, profile.SecurityVersion);

        return AuthResponse.Create(token, DateTime.UtcNow.AddMinutes(15), profile);
    }
}