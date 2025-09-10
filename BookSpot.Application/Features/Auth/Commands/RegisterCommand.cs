using BookSpot.Application.DTOs.Auth;
using MediatR;

namespace BookSpot.Application.Features.Auth.Commands;

public record RegisterCommand(string Email, string Password, string UserType) : IRequest<AuthResponse>;