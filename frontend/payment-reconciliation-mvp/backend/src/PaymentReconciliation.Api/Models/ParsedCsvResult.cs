namespace PaymentReconciliation.Api.Models;

public class ParsedCsvResult
{
    public List<TransactionRecord> Records { get; init; } = [];
    public List<ParseError> Errors { get; init; } = [];
}
