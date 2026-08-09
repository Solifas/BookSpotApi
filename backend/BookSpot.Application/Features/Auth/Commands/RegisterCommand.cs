using BookSpot.Application.DTOs.Auth;
using MediatR;

namespace BookSpot.Application.Features.Auth.Commands;

public record RegisterCommand(string Email, string FullName, string? ContactNumber, string Password, string UserType) : IRequest<AuthResponse>;