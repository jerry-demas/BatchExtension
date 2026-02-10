namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;

public record GfrEndPointOptions
{
    public required string TaxYear { get; init; }
    public required string ServiceType { get; init; }
    public required string Domain { get; init; }
    public required string Auth { get; init; }
    public required string CreateDocument { get; init; }
    public required string GetIndexes { get; init; }    
    public required string GetClientsByDrawerId { get; init; }
    public required string UploadDocument { get; init; }    
    public required string TrackingReportByWorkflow { get; init; }
    public required string EditWorkFlow {get; init;}    
}
