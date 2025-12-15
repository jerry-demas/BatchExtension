namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record BatchItemStatus(
    Guid ItemGuid,
    string ItemStatusCode,
    string ItemStatusDescription,
    string ResponseCode,
    string ResponseDescription,
    ReturnInfo ReturnInfo,
    InfoPair[] AdditionalInfo
);
