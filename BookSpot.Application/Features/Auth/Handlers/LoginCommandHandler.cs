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
        // Find user by email (you'll need to add this method to IProfileRepository)
        var profile = await _profileRepository.GetByEmailAsync(request.Email);

        if (profile == null)
        {
            throw new ValidationException("Invalid email or password");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, profile.PasswordHash))
        {
            throw new ValidationException("Invalid email or password");
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(profile.Id, profile.Email, profile.UserType);

        return new AuthResponse
        {
            Token = token,
            UserId = profile.Id,
            Email = profile.Email,
            FullName = profile.FullName,
            ContactNumber = profile.ContactNumber,
            UserType = profile.UserType,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Match with JWT expiration
        };
    }
}