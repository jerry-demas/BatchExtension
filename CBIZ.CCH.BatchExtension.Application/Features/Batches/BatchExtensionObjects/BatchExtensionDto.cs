namespace CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchExtensionObjects;

public record BatchExtensionDto
(
    int Id,
    string FirmFlowId,
    string? TaxReturnId,
    string SubmittedBy,
    DateTime? SubmittedDate,
    Guid? BatchId,
    Guid? BatchItemGuid,
    string? BatchItemStatus,
    string? StatusDescription,
    DateTime? UpdatedDate
);
