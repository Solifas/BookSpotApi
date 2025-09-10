using FluentValidation;

namespace BookSpot.Application.Features.Businesses.Commands;

public class CreateBusinessCommandValidator : AbstractValidator<CreateBusinessCommand>
{
    public CreateBusinessCommandValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("Business name is required.")
            .Length(2, 100)
            .WithMessage("Business name must be between 2 and 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-&'.,()]+$")
            .WithMessage("Business name contains invalid characters.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.")
            .Length(2, 50)
            .WithMessage("City must be between 2 and 50 characters.")
            .Matches(@"^[a-zA-Z\s\-'.,()]+$")
            .WithMessage("City contains invalid characters.");
    }
}