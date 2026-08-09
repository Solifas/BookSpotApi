using PaymentReconciliation.Api.Models;

namespace PaymentReconciliation.Api.Services;

public class ReconciliationEngine : IReconciliationEngine
{
    private const int MaxDateDriftDays = 2;

    public ReconciliationResult Reconcile(List<TransactionRecord> bankTransactions, List<TransactionRecord> systemTransactions, List<ParseError>? parseErrors = null)
    {
        var result = new ReconciliationResult();
        if (parseErrors is not null)
        {
            result.ParseErrors.AddRange(parseErrors);
        }

        var unmatchedSystems = new HashSet<string>(systemTransactions.Select(t => t.Id));

        foreach (var bank in bankTransactions)
        {
            var candidates = systemTransactions
                .Where(system => unmatchedSystems.Contains(system.Id))
                .Where(system => system.Amount == bank.Amount)
                .Where(system => Math.Abs(system.Date.DayNumber - bank.Date.DayNumber) <= MaxDateDriftDays)
                .ToList();

            if (candidates.Count == 0)
            {
                result.MissingInSystem.Add(new UnmatchedRecord
                {
                    Transaction = bank,
                    Reason = bank.IsDuplicate ? "No system match found (duplicate bank record)." : "No system match found."
                });
                continue;
            }

            var bestCandidate = PickBest(bank, candidates);
            unmatchedSystems.Remove(bestCandidate.Id);

            var dateDiff = Math.Abs(bestCandidate.Date.DayNumber - bank.Date.DayNumber);
            var reason = BuildMatchReason(bank, bestCandidate, dateDiff);

            result.Matched.Add(new MatchResult
            {
                BankTransaction = bank,
                SystemTransaction = bestCandidate,
                MatchReason = reason,
                DateDifferenceDays = dateDiff
            });
        }

        foreach (var system in systemTransactions.Where(t => unmatchedSystems.Contains(t.Id)))
        {
            result.MissingInBank.Add(new UnmatchedRecord
            {
                Transaction = system,
                Reason = system.IsDuplicate ? "No bank match found (duplicate system record)." : "No bank match found."
            });
        }

        return result;
    }

    private static TransactionRecord PickBest(TransactionRecord bank, List<TransactionRecord> candidates)
    {
        var normalizedBankRef = NormalizeRef(bank.Reference);

        return candidates
            .OrderByDescending(candidate => !string.IsNullOrEmpty(normalizedBankRef) && NormalizeRef(candidate.Reference) == normalizedBankRef)
            .ThenBy(candidate => Math.Abs(candidate.Date.DayNumber - bank.Date.DayNumber))
            .ThenBy(candidate => candidate.SourceRowNumber)
            .First();
    }

    private static string BuildMatchReason(TransactionRecord bank, TransactionRecord system, int dateDiff)
    {
        var bankRef = NormalizeRef(bank.Reference);
        var systemRef = NormalizeRef(system.Reference);

        if (!string.IsNullOrWhiteSpace(bankRef) && bankRef == systemRef)
        {
            return "Matched on amount + date tolerance + exact reference";
        }

        return dateDiff == 0
            ? "Matched on amount + exact date"
            : "Matched on amount + nearest date in tolerance";
    }

    private static string? NormalizeRef(string? reference)
        => string.IsNullOrWhiteSpace(reference) ? null : reference.Trim().ToLowerInvariant();
}
