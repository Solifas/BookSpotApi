namespace PaymentReconciliation.Api.Contracts;

public class UploadResponse
{
    public required string Source { get; init; }
    public int AcceptedRows { get; init; }
    public int ParseErrorCount { get; init; }
    public List<string> Warnings { get; init; } = [];
}
