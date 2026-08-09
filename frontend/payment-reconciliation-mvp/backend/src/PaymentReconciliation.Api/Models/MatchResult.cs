namespace PaymentReconciliation.Api.Models;

public class MatchResult
{
    public required TransactionRecord BankTransaction { get; init; }
    public required TransactionRecord SystemTransaction { get; init; }
    public required string MatchReason { get; init; }
    public int DateDifferenceDays { get; init; }
}
