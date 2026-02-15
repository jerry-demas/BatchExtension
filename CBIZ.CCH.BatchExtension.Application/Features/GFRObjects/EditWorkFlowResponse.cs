namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record EditWorkFlowResponse
(
    int FilingId,
    string ErrorComment,
    bool Status,
    List<ErrorMessage> ErrorMessage
);


