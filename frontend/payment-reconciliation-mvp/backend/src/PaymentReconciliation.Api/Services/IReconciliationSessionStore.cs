using PaymentReconciliation.Api.Models;

namespace PaymentReconciliation.Api.Services;

public interface IReconciliationSessionStore
{
    ReconciliationSession Get();
    void SetBankTransactions(List<TransactionRecord> records, List<ParseError> errors);
    void SetSystemTransactions(List<TransactionRecord> records, List<ParseError> errors);
    void SetResult(ReconciliationResult result);
}
