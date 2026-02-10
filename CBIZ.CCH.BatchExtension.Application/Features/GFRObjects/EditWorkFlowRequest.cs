using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

namespace CBIZ.CCH.BatchExtension.Application;

public record EditWorkflowRequest(
    int FilingId,
    InformationField InformationFields
);
