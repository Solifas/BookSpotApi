using PaymentReconciliation.Api.Models;

namespace PaymentReconciliation.Api.Services;

public interface IReconciliationEngine
{
    ReconciliationResult Reconcile(List<TransactionRecord> bankTransactions, List<TransactionRecord> systemTransactions, List<ParseError>? parseErrors = null);
}
