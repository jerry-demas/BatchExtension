namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record EditWorkFlowResponse
(
    string FilingId,
    string ErrorComment,
    string Status,
    List<ErrorMessage> ErrorMessage
);


