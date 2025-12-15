namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record GetBatchStatusResponse(string BatchStatus, string BatchStatusDescription, BatchItemStatus[] items);
