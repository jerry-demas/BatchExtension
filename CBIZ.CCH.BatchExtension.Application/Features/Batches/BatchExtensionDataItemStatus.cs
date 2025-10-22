namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record BatchExtensionDataItemStatus(string Code, string Description)
{
    public static readonly BatchExtensionDataItemStatus Added = new("add", "Added");
    public static readonly BatchExtensionDataItemStatus StatusGood = new("complete", "Batch Status Complete");
    public static readonly BatchExtensionDataItemStatus StatusBad = new("error", "Batch Status Error");
    public static readonly BatchExtensionDataItemStatus CchCreate = new("creating", "CCH Creating");
    public static readonly BatchExtensionDataItemStatus CchDownloadGood = new("download", "Downloaded from CCH");
    public static readonly BatchExtensionDataItemStatus CchDownloadError = new("downloadErr", "Error CCH downlaod");
    public static readonly BatchExtensionDataItemStatus GfrUploadGood = new("upload", "Uploaded to GFR");
    public static readonly BatchExtensionDataItemStatus GfrUploadError = new("uploadErr", "Error GFR upload");   

    public static IEnumerable<BatchExtensionDataItemStatus> List() =>
            new[] { Added, StatusGood, StatusBad, CchCreate,CchDownloadGood,CchDownloadError,GfrUploadGood, GfrUploadError };

}