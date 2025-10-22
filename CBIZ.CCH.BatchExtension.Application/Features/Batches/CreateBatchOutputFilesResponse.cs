namespace CBIZ.CCH.BatchExtension.Application;

public record CreateBatchOutputFilesResponse(Guid BatchItemGuid, string FileName, int Length);
