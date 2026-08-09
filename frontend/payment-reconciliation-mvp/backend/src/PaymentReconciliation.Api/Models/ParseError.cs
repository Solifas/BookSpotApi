namespace PaymentReconciliation.Api.Models;

public class ParseError
{
    public int RowNumber { get; init; }
    public required string Message { get; init; }
    public string? RawData { get; init; }
}
