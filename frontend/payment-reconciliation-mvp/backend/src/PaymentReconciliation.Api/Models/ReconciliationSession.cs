namespace PaymentReconciliation.Api.Models;

public class ReconciliationSession
{
    public List<TransactionRecord> BankTransactions { get; set; } = [];
    public List<TransactionRecord> SystemTransactions { get; set; } = [];
    public List<ParseError> ParseErrors { get; set; } = [];
    public ReconciliationResult? LastResult { get; set; }
}
