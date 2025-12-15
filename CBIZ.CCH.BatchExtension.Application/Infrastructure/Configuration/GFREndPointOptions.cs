namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public record GfrEndPointOptions
{
    public required string Domain { get; init; }
    public required string Auth { get; init; }
    public required string CreateDocument { get; init; }
    public required string GetIndexes { get; init; }    
    public required string GetClientsByDrawerId { get; init; }
    public required string UploadDocument { get; init; }
    public required string UploadDocumentToTaxReturn { get; init; }
}
