using FluentValidation;

namespace BookSpot.Application.Features.Reviews.Commands;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .Must(ValidOpaqueId)
            .WithMessage("Review ID must be 1-128 UTF-8 bytes without control characters.");
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
        RuleFor(command => command.Comment)
            .NotEmpty()
            .MaximumLength(2000)
            .Must(ValidComment)
            .WithMessage("Comment contains invalid control characters.");
    }

    private static bool ValidOpaqueId(string value) =>
        System.Text.Encoding.UTF8.GetByteCount(value) <= 128 && value.All(character => !char.IsControl(character));

    private static bool ValidComment(string value) =>
        value.All(character => character == '\n' || !char.IsControl(character));
}
