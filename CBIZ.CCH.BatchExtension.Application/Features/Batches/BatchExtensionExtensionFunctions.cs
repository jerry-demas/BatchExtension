using CBIZ.CCH.BatchExtension.Application.Shared;

using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;
using Microsoft.Identity.Client.Advanced;
using Azure.Core;

namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public static class BatchExtensionExtensionFunctions
{

    public static bool ValidateReturnIds(this List<string> returnIds)
    {
        if (returnIds == null || returnIds.Count == 0)
            return false;

        var first = returnIds[0];

        if (first.Length < 5)
            return false;

        var targetYear = first.Substring(0, 4);
        var targetType = first[4];

        return returnIds.All(s =>
            s.Length >= 5 &&
            s.Substring(0, 4) == targetYear &&
            s[4] == targetType
        );
    }


    public static void UpdateBatchItemsGuidAndFileName(this List<BatchExtensionData> batch, List<CreateBatchOutputFilesResponse> response)
    {
        var responseList = response
           .Select(x => new
           {
               BatchItemGuid = x.BatchItemGuid,
               FileName = x.FileName,
               ReturnId = RegexReplace.FileNameToReturnID().Replace(x.FileName, "$1P:$2:V$3"),
               Length = x.Length
           }).ToList();

        var updateDict = responseList.ToDictionary(u => u.ReturnId, u => u);
        foreach (var batchItem in batch)
        {
            if (updateDict.TryGetValue(batchItem.TaxReturnId, out var BatchItemObject))
            {
                batchItem.BatchItemGuid = BatchItemObject.BatchItemGuid;
                batchItem.FileName = BatchItemObject.FileName;
            }

        }

    }
    public static List<BatchItemForEmail> ConvertForEmailList(this List<BatchExtensionData> BatchItems)
        => BatchItems.Select(x => new BatchItemForEmail(
            x.FirmFlowId,
            x.TaxReturnId, 
            x.BatchItemStatus, 
            x.StatusDescription)
        ).ToList();

    public static List<BatchExtensionData> ConvertToBatchExtensionData(
        this LaunchBatchRunRequest request,
        Guid queueId,
        Guid batchGuid
    )
    {
        return request.Returns.SelectMany(r => r.FirmFlowId.Select(firmFlowId =>
            new BatchExtensionData
            {
                Id = Guid.Empty,
                QueueIDGUID = queueId,
                FirmFlowId = firmFlowId,
                TaxReturnId = r.ReturnId,
                ClientName = r.ClientName,
                ClientNumber = r.ClientNumber,
                OfficeLocation = r.OfficeLocation,
                EngagementType = r.EngagementType,
                BatchId = batchGuid,
                BatchItemGuid = Guid.Empty,
                BatchItemStatus = BatchExtensionDataItemStatus.Added.Code,
                StatusDescription = BatchExtensionDataItemStatus.Added.Description,
                FileName = string.Empty,
                FileDownLoadedFromCCH = false,
                FileUploadedToGFR = false,
                GfrDocumentId = string.Empty,
                CreationDate = DateTime.Now,
                UpdatedDate = DateTime.Now
        })).ToList();
    }

    public static void UpdateExtensionDataDbFrom(this BatchExtensionData batchFromDb, BatchExtensionData batchUpdated)
    {
        batchFromDb.FirmFlowId = batchUpdated.FirmFlowId;
        batchFromDb.TaxReturnId = batchUpdated.TaxReturnId;
        batchFromDb.BatchId = batchUpdated.BatchId;
        batchFromDb.BatchItemGuid = batchUpdated.BatchItemGuid;
        batchFromDb.BatchItemStatus = batchUpdated.BatchItemStatus;
        batchFromDb.StatusDescription = batchUpdated.StatusDescription;
        batchFromDb.FileName = batchUpdated.FileName;
        batchFromDb.UpdatedDate = batchUpdated.UpdatedDate ?? DateTime.UtcNow;
    }

    public static void UpdateExtensionQueueDbFrom(this BatchExtensionQueue batchFromDb, BatchExtensionQueue batchUpdated)
    {
        batchFromDb.QueueId = batchUpdated.QueueId;
        batchFromDb.QueueRequest = batchUpdated.QueueRequest;
        batchFromDb.QueueStatus = batchUpdated.QueueStatus;
        batchFromDb.BatchStatus = batchUpdated.BatchStatus;
        batchFromDb.SubmittedBy = batchUpdated.SubmittedBy;
        batchFromDb.SubmittedBy = batchUpdated.SubmittedBy;

    }
    
    

}
