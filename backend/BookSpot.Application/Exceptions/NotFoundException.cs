namespace BookSpot.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base("The requested resource was not found.")
    {
    }

    public NotFoundException(string name, object key)
        : base("The requested resource was not found.")
    {
    }
}