
namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

public class BatchExtensionData
{
    public Guid Id { get; set; } 
    public Guid QueueIDGUID { get; set; } 
    public string FirmFlowId { get; set; } = string.Empty;
    public string TaxReturnId { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public Guid BatchItemGuid { get; set; }
    public string BatchItemStatus { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool FileDownLoadedFromCCH { get; set; }
    public bool FileUploadedToGFR { get; set; }
    public DateTime? UpdatedDate { get; set; }
   
}
