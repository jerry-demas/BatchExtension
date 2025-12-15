namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public record CchEndPointOptions
{
    public required string Domain { get; init; }
    public required string CreateBatchAPI { get; init; }
    public required string GetBatchStatusAPI { get; init; }
    public required string GetBatchOutputFilesAPI { get; init; }
    public required string BatchOutputDownloadFileAPI { get; init; }
}
