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
        // Check if user already exists
        var existingProfile = await _profileRepository.GetByEmailAsync(request.Email);
        if (existingProfile != null)
        {
            throw new ValidationException("User with this email already exists");
        }

        // Validate user type
        if (request.UserType != "client" && request.UserType != "provider")
        {
            throw new ValidationException("UserType must be either 'client' or 'provider'");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create new profile
        var profile = new Profile
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            FullName = request.FullName,
            ContactNumber = request.ContactNumber,
            UserType = request.UserType,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _profileRepository.SaveAsync(profile);

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
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }
}