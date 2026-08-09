using PaymentReconciliation.Api.Models;
using PaymentReconciliation.Api.Services;

namespace PaymentReconciliation.Tests;

public class ReconciliationEngineTests
{
    private readonly ReconciliationEngine _engine = new();

    [Fact]
    public void Reconcile_MatchesByReference_WhenMultipleCandidatesExist()
    {
        var bank = new TransactionRecord
        {
            Id = "b1", Date = new DateOnly(2026, 1, 10), Amount = 100m, Reference = "INV-100", SourceType = "bank", SourceRowNumber = 2
        };

        var systemA = new TransactionRecord
        {
            Id = "s1", Date = new DateOnly(2026, 1, 9), Amount = 100m, Reference = "OTHER", SourceType = "system", SourceRowNumber = 2
        };

        var systemB = new TransactionRecord
        {
            Id = "s2", Date = new DateOnly(2026, 1, 11), Amount = 100m, Reference = "inv-100", SourceType = "system", SourceRowNumber = 3
        };

        var result = _engine.Reconcile([bank], [systemA, systemB]);

        Assert.Single(result.Matched);
        Assert.Equal("s2", result.Matched[0].SystemTransaction.Id);
        Assert.Empty(result.MissingInBank);
        Assert.Empty(result.MissingInSystem);
    }

    [Fact]
    public void Reconcile_UsesClosestDate_WhenReferenceNotAvailable()
    {
        var bank = new TransactionRecord
        {
            Id = "b1", Date = new DateOnly(2026, 1, 10), Amount = 50m, SourceType = "bank", SourceRowNumber = 2
        };

        var systemFar = new TransactionRecord
        {
            Id = "s1", Date = new DateOnly(2026, 1, 8), Amount = 50m, SourceType = "system", SourceRowNumber = 2
        };

        var systemClose = new TransactionRecord
        {
            Id = "s2", Date = new DateOnly(2026, 1, 10), Amount = 50m, SourceType = "system", SourceRowNumber = 3
        };

        var result = _engine.Reconcile([bank], [systemFar, systemClose]);

        Assert.Single(result.Matched);
        Assert.Equal("s2", result.Matched[0].SystemTransaction.Id);
    }

    [Fact]
    public void Reconcile_ProducesUnmatchedCollections()
    {
        var bank = new TransactionRecord
        {
            Id = "b1", Date = new DateOnly(2026, 1, 1), Amount = 10m, SourceType = "bank", SourceRowNumber = 2
        };

        var system = new TransactionRecord
        {
            Id = "s1", Date = new DateOnly(2026, 1, 20), Amount = 10m, SourceType = "system", SourceRowNumber = 2
        };

        var result = _engine.Reconcile([bank], [system]);

        Assert.Empty(result.Matched);
        Assert.Single(result.MissingInSystem);
        Assert.Single(result.MissingInBank);
    }
}
