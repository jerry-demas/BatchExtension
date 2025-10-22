namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record FileResult(int FIleGroupID, bool IsError, string[] messages, string[]subItemExecutionIDs);
