namespace BookSpot.Application.Exceptions;

public sealed class ConflictException(string message, string code = "booking_state_conflict") : Exception(message)
{
    public string Code { get; } = code;
}