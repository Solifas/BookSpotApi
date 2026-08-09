namespace BookSpot.Application.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BadRequestException(string name, object value)
        : base($"Bad request for {name}: '{value}' is invalid.")
    {
    }

    public BadRequestException(string name, object value, string reason)
        : base($"Bad request for {name}: '{value}' is invalid. {reason}")
    {
    }
}