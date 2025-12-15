
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

public class BatchExtensionData
{
    public Guid Id { get; set; } 
    public Guid QueueIDGUID { get; set; } 
    public string FirmFlowId { get; set; } = string.Empty;
    public string TaxReturnId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string OfficeLocation { get; set; } = string.Empty;
    public string EngagementType {get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public Guid BatchItemGuid { get; set; }
    public string BatchItemStatus { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool FileDownLoadedFromCCH { get; set; }
    public bool FileUploadedToGFR { get; set; }
    public string GfrDocumentId {get; set; } = string.Empty;
    public DateTime CreationDate {get; set;}    
    public DateTime? UpdatedDate { get; set; }
    public BatchExtensionQueue? Queue { get; init; }
   
}
