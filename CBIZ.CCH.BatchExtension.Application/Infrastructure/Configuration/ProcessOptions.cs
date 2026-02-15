namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public record ProcessOptions
{
    public int StatusTimeIntervalSeconds { get; init; }
    public int StatusRetryLimit { get; init; }
    public required string DownloadFilesDirectory { get; init; }
    public bool UseCchMockData { get; init; }
    public int GfrUploadRetryLimit {get; init;}
    public List<string> AllowedGfrWorkFlows { get; set; } = new(); 
}