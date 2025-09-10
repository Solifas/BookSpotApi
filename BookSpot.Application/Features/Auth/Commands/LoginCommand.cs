using BookSpot.Application.DTOs.Auth;
using MediatR;

namespace BookSpot.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;