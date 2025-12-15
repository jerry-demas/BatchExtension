using CBIZ.CCH.BatchExtension.Application.Features.Batches;

namespace CBIZ.CCH.BatchExtension.Application;

public static class CchMockData
{
    public static Guid TestBatchId() => Guid.NewGuid();
    public static (List<BatchItemStatus>, string) TestBatchStatusDescription() => (new List<BatchItemStatus>(), BatchRecordStatus.Complete.Description);
    public static List<CreateBatchOutputFilesResponse> TestBatchCreateBatchOutputFilesResponse()
    => 
    [
        new  CreateBatchOutputFilesResponse(Guid.NewGuid(), "2024US P2326033 Extensions V1.pdf", 0)
    ];
}
