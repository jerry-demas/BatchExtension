namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public record ProcessOptions
{
    public int StatusTimeIntervalSeconds { get; init; }
    public int StatusRetryLimit { get; init; }
    public required string DownloadFilesDirectory { get; init; }
}