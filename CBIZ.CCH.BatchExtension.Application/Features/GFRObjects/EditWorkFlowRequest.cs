namespace CBIZ.CCH.BatchExtension.Application.Features.GFRObjects;

public record EditWorkflowRequest(
    int FilingId,
    EditWorkFlowDeliverable Deliverable
);
