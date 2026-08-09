namespace PaymentReconciliation.Api.Models;

public class UnmatchedRecord
{
    public required TransactionRecord Transaction { get; init; }
    public required string Reason { get; init; }
}
