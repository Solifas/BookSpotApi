namespace PaymentReconciliation.Api.Models;

public class ReconciliationResult
{
    public List<MatchResult> Matched { get; init; } = [];
    public List<UnmatchedRecord> MissingInBank { get; init; } = [];
    public List<UnmatchedRecord> MissingInSystem { get; init; } = [];
    public List<ParseError> ParseErrors { get; init; } = [];
    public DateTimeOffset GeneratedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
