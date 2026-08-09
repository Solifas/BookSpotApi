using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PaymentReconciliation.Api.Models;

namespace PaymentReconciliation.Api.Services;

public class CsvParsingService : ICsvParsingService
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "M/d/yyyy", "d/M/yyyy"
    ];

    public async Task<ParsedCsvResult> ParseAsync(IFormFile file, string sourceType, CancellationToken cancellationToken = default)
    {
        var result = new ParsedCsvResult();

        if (file.Length == 0)
        {
            result.Errors.Add(new ParseError { RowNumber = 0, Message = "Uploaded file is empty." });
            return result;
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant(),
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            result.Errors.Add(new ParseError { RowNumber = 0, Message = "CSV file has no header row." });
            return result;
        }

        var headers = csv.HeaderRecord?.Select(h => h.Trim().ToLowerInvariant()).ToHashSet() ?? [];
        if (!headers.Contains("date") || !headers.Contains("amount"))
        {
            result.Errors.Add(new ParseError { RowNumber = 0, Message = "CSV must include Date and Amount columns." });
            return result;
        }

        var row = 1;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            row++;

            try
            {
                var dateRaw = GetField(csv, "date");
                var amountRaw = GetField(csv, "amount");
                var referenceRaw = GetField(csv, "reference");
                var descriptionRaw = GetField(csv, "description");

                if (string.IsNullOrWhiteSpace(dateRaw) || string.IsNullOrWhiteSpace(amountRaw))
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = row,
                        Message = "Date and Amount are required.",
                        RawData = csv.Context.Parser.RawRecord
                    });
                    continue;
                }

                if (!TryParseDate(dateRaw, out var date))
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = row,
                        Message = $"Invalid date format: '{dateRaw}'.",
                        RawData = csv.Context.Parser.RawRecord
                    });
                    continue;
                }

                if (!TryParseAmount(amountRaw, out var amount))
                {
                    result.Errors.Add(new ParseError
                    {
                        RowNumber = row,
                        Message = $"Invalid amount format: '{amountRaw}'.",
                        RawData = csv.Context.Parser.RawRecord
                    });
                    continue;
                }

                result.Records.Add(new TransactionRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Date = date,
                    Amount = amount,
                    Reference = NormalizeString(referenceRaw),
                    Description = NormalizeString(descriptionRaw),
                    SourceRowNumber = row,
                    SourceType = sourceType
                });
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ParseError
                {
                    RowNumber = row,
                    Message = $"Unexpected parse error: {ex.Message}",
                    RawData = csv.Context.Parser.RawRecord
                });
            }
        }

        MarkDuplicates(result.Records);
        return result;
    }

    private static string? GetField(CsvReader csv, string name)
    {
        try
        {
            return csv.GetField(name);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseDate(string input, out DateOnly date)
    {
        input = input.Trim();
        if (DateOnly.TryParseExact(input, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        return false;
    }

    private static bool TryParseAmount(string input, out decimal amount)
    {
        var sanitized = input.Trim().Replace(" ", string.Empty);
        if (decimal.TryParse(sanitized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out amount))
        {
            return true;
        }

        sanitized = sanitized.Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(sanitized, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out amount);
    }

    private static string? NormalizeString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void MarkDuplicates(List<TransactionRecord> records)
    {
        var duplicateGroups = records
            .GroupBy(r => new
            {
                r.Date,
                r.Amount,
                Reference = r.Reference?.Trim().ToLowerInvariant(),
                Description = r.Description?.Trim().ToLowerInvariant()
            })
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            foreach (var record in group)
            {
                record.IsDuplicate = true;
            }
        }
    }
}
