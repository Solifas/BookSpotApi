using BookSpot.Domain.Entities;
using BookSpot.Application.Abstractions.Repositories;
using BookSpot.Application.Abstractions.Services;
using BookSpot.Application.Exceptions;
using MediatR;

namespace BookSpot.Application.Features.BusinessHours.Commands;

public record CreateBusinessHourCommand(string BusinessId, int DayOfWeek, string OpenTime, string CloseTime, bool IsClosed) : IRequest<BusinessHour>;

public class CreateBusinessHourHandler : IRequestHandler<CreateBusinessHourCommand, BusinessHour>
{
    private readonly IBusinessHourRepository _businessHours;
    private readonly IBusinessRepository _businesses;
    private readonly IClaimsService _claimsService;

    public CreateBusinessHourHandler(IBusinessHourRepository businessHours, IBusinessRepository businesses, IClaimsService claimsService)
    {
        _businessHours = businessHours;
        _businesses = businesses;
        _claimsService = claimsService;
    }

    public async Task<BusinessHour> Handle(CreateBusinessHourCommand request, CancellationToken cancellationToken)
    {
        // Get current user from JWT claims
        var currentUserId = _claimsService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new ValidationException("User must be authenticated to create business hours.");
        }

        // Validate that the current user is a provider
        if (!_claimsService.IsProvider())
        {
            throw new ValidationException("Only providers can create business hours.");
        }

        // Validate that the business exists
        var business = await _businesses.GetAsync(request.BusinessId);
        if (business == null)
        {
            throw new NotFoundException($"Business with ID '{request.BusinessId}' not found.");
        }

        // Validate that the current user owns the business
        if (business.ProviderId != currentUserId)
        {
            throw new ValidationException("You can only create business hours for your own businesses.");
        }

        // Validate day of week
        if (request.DayOfWeek < 0 || request.DayOfWeek > 6)
        {
            throw new ValidationException("Day of week must be between 0 (Sunday) and 6 (Saturday).");
        }

        // Validate time format if not closed
        if (!request.IsClosed)
        {
            if (string.IsNullOrWhiteSpace(request.OpenTime) || string.IsNullOrWhiteSpace(request.CloseTime))
            {
                throw new ValidationException("Open time and close time are required when the business is not closed.");
            }

            // Basic time format validation (you might want to add more sophisticated validation)
            if (!TimeSpan.TryParse(request.OpenTime, out _) || !TimeSpan.TryParse(request.CloseTime, out _))
            {
                throw new ValidationException("Open time and close time must be in valid time format (HH:mm).");
            }
        }

        var businessHour = new BusinessHour
        {
            Id = Guid.NewGuid().ToString(),
            BusinessId = request.BusinessId,
            DayOfWeek = request.DayOfWeek,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime,
            IsClosed = request.IsClosed
        };

        await _businessHours.SaveAsync(businessHour);
        return businessHour;
    }
}