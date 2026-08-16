using BookSpot.Application.Abstractions.Repositories;
using MediatR;

namespace BookSpot.Application.Features.Auth.Commands;

public sealed record ValidateResetTokenRequest(string Token);
public sealed record ValidateResetTokenQuery(string Token) : IRequest<bool>;

public sealed class ValidateResetTokenHandler(IPasswordResetTokenRepository tokens)
    : IRequestHandler<ValidateResetTokenQuery, bool>
{
    public async Task<bool> Handle(ValidateResetTokenQuery request, CancellationToken cancellationToken)
    {
        if (!ResetTokenRules.IsWellFormed(request.Token))
        {
            return false;
        }

        var token = await tokens.GetAsync(ResetTokenRules.Digest(request.Token));
        return token is not null && !token.IsUsed && token.ExpiresAt > DateTime.UtcNow;
    }
}
