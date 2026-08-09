using PaymentReconciliation.Api.Models;

namespace PaymentReconciliation.Api.Services;

public interface ICsvParsingService
{
    Task<ParsedCsvResult> ParseAsync(IFormFile file, string sourceType, CancellationToken cancellationToken = default);
}
