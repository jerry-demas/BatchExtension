namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record BatchItemForEmail(
    string FirmFlowId,
    string TaxReturnId,
    string ItemStatus,
    string StatusDescription
);