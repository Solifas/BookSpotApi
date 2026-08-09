using Microsoft.AspNetCore.Mvc;
using PaymentReconciliation.Api.Contracts;
using PaymentReconciliation.Api.Services;

namespace PaymentReconciliation.Api.Controllers;

[ApiController]
[Route("api")]
public class ReconciliationController(
    ICsvParsingService csvParsingService,
    IReconciliationEngine reconciliationEngine,
    IReconciliationSessionStore store) : ControllerBase
{
    [HttpPost("upload/bank")]
    public Task<IActionResult> UploadBank([FromForm] IFormFile? file, CancellationToken cancellationToken)
        => Upload(file, "bank", cancellationToken);

    [HttpPost("upload/system")]
    public Task<IActionResult> UploadSystem([FromForm] IFormFile? file, CancellationToken cancellationToken)
        => Upload(file, "system", cancellationToken);

    [HttpPost("reconcile")]
    public IActionResult Reconcile()
    {
        var session = store.Get();
        if (session.BankTransactions.Count == 0 || session.SystemTransactions.Count == 0)
        {
            return BadRequest(new
            {
                error = "Both bank and system CSV files must be uploaded before reconciliation.",
                bankCount = session.BankTransactions.Count,
                systemCount = session.SystemTransactions.Count
            });
        }

        var result = reconciliationEngine.Reconcile(session.BankTransactions, session.SystemTransactions, session.ParseErrors);
        store.SetResult(result);

        return Ok(result);
    }

    [HttpGet("results")]
    public IActionResult GetResults()
    {
        var session = store.Get();
        return session.LastResult is null
            ? NotFound(new { error = "No reconciliation has been run yet." })
            : Ok(session.LastResult);
    }

    private async Task<IActionResult> Upload(IFormFile? file, string source, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new { error = "File is required." });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only CSV files are supported." });
        }

        var parsed = await csvParsingService.ParseAsync(file, source, cancellationToken);
        if (source == "bank")
        {
            store.SetBankTransactions(parsed.Records, parsed.Errors);
        }
        else
        {
            store.SetSystemTransactions(parsed.Records, parsed.Errors);
        }

        var warnings = new List<string>();
        if (parsed.Errors.Count > 0)
        {
            warnings.Add($"{parsed.Errors.Count} row(s) failed parsing and were skipped.");
        }

        return Ok(new UploadResponse
        {
            Source = source,
            AcceptedRows = parsed.Records.Count,
            ParseErrorCount = parsed.Errors.Count,
            Warnings = warnings
        });
    }
}
