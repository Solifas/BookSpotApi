namespace PaymentReconciliation.Api.Models;

public class TransactionRecord
{
    public required string Id { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal Amount { get; init; }
    public string? Reference { get; init; }
    public string? Description { get; init; }
    public int SourceRowNumber { get; init; }
    public required string SourceType { get; init; }
    public bool IsDuplicate { get; set; }
}
