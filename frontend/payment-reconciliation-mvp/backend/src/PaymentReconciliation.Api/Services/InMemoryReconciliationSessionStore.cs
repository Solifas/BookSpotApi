using PaymentReconciliation.Api.Models;

namespace PaymentReconciliation.Api.Services;

public class InMemoryReconciliationSessionStore : IReconciliationSessionStore
{
    private readonly ReconciliationSession _session = new();
    private readonly object _lock = new();

    public ReconciliationSession Get()
    {
        lock (_lock)
        {
            return new ReconciliationSession
            {
                BankTransactions = [.. _session.BankTransactions],
                SystemTransactions = [.. _session.SystemTransactions],
                ParseErrors = [.. _session.ParseErrors],
                LastResult = _session.LastResult
            };
        }
    }

    public void SetBankTransactions(List<TransactionRecord> records, List<ParseError> errors)
    {
        lock (_lock)
        {
            _session.BankTransactions = records;
            _session.ParseErrors.RemoveAll(x => x.Message.StartsWith("Bank:", StringComparison.OrdinalIgnoreCase));
            _session.ParseErrors.AddRange(errors.Select(e => new ParseError
            {
                RowNumber = e.RowNumber,
                RawData = e.RawData,
                Message = $"Bank: {e.Message}"
            }));
        }
    }

    public void SetSystemTransactions(List<TransactionRecord> records, List<ParseError> errors)
    {
        lock (_lock)
        {
            _session.SystemTransactions = records;
            _session.ParseErrors.RemoveAll(x => x.Message.StartsWith("System:", StringComparison.OrdinalIgnoreCase));
            _session.ParseErrors.AddRange(errors.Select(e => new ParseError
            {
                RowNumber = e.RowNumber,
                RawData = e.RawData,
                Message = $"System: {e.Message}"
            }));
        }
    }

    public void SetResult(ReconciliationResult result)
    {
        lock (_lock)
        {
            _session.LastResult = result;
        }
    }
}
