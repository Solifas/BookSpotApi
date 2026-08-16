using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.DTOs.Auth;
using BookSpot.Application.Exceptions;
using BookSpot.Application.Features.Auth.Commands;
using BookSpot.Domain.Entities;
using MediatR;

namespace BookSpot.Application.Features.Auth.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(IProfileRepository profileRepository, IJwtService jwtService)
    {
        _profileRepository = profileRepository;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = AuthRules.NormalizeEmail(request.Email);
        // Validate UserType
        if (request.UserType != "client" && request.UserType != "provider")
        {
            throw new ValidationException("UserType must be either 'client' or 'provider'");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        // Create new profile
        var profile = new Profile
        {
            Id = Guid.NewGuid().ToString(),
            Email = normalizedEmail,
            EmailNormalized = normalizedEmail,
            FullName = request.FullName,
            ContactNumber = request.ContactNumber,
            UserType = request.UserType,
            PasswordHash = passwordHash,
            SecurityVersion = 1,
            CreatedAt = DateTime.UtcNow
        };

        if (!await _profileRepository.CreateAsync(profile))
        {
            throw new ConflictException("User with this email already exists");
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(profile.Id, profile.Email, profile.UserType, profile.SecurityVersion);

        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        return AuthResponse.Create(token, expiresAt, profile);
    }
}