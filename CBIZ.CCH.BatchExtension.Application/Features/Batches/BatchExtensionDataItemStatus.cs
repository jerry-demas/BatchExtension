namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record BatchExtensionDataItemStatus(string Code, string Description)
{
    public static readonly BatchExtensionDataItemStatus Added = new("add", "Added");
    public static readonly BatchExtensionDataItemStatus StatusGood = new("complete", "Batch Status Complete");
    public static readonly BatchExtensionDataItemStatus StatusBad = new("error", "Batch Status Error");
    public static readonly BatchExtensionDataItemStatus CchBatchCreated = new("batchCreated", "CCH Batch Created");
    public static readonly BatchExtensionDataItemStatus CchBatchCreatedError = new("batchCreatedErr", "CCH Batch Created Error");
    public static readonly BatchExtensionDataItemStatus CchDownloadGood = new("download", "Downloaded from CCH");
    public static readonly BatchExtensionDataItemStatus CchDownloadError = new("downloadErr", "Error CCH download");    
    public static readonly BatchExtensionDataItemStatus GfrDocumentCreated = new ("gfrDocumentCreated", "GFR Document created");    
    public static readonly BatchExtensionDataItemStatus GfrUploadGood = new("upload", "Uploaded to GFR");
    public static readonly BatchExtensionDataItemStatus GfrUploadError = new("uploadErr", "Error GFR upload");   
    public static readonly BatchExtensionDataItemStatus GfrDueDateExtendedGood = new ("dueDateExtended", "Due Date Extended");
    public static readonly BatchExtensionDataItemStatus GfrDueDateExtendedError = new ("dueDateExtendedErr", "Error Due Date Extension");


    public static IEnumerable<BatchExtensionDataItemStatus> List() =>
            new[] { 
                Added, 
                StatusGood, 
                StatusBad, 
                CchBatchCreated, 
                CchBatchCreatedError,
                CchDownloadGood,
                CchDownloadError, 
                GfrDocumentCreated, 
                GfrUploadGood, 
                GfrUploadError, 
                GfrDueDateExtendedGood, 
                GfrDueDateExtendedError 
            };
}