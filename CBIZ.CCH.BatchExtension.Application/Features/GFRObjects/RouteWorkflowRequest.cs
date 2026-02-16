namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record RouteWorkflowRequest
(
    List<int> FilingId,
    bool Complete,
    string NextStep,
    string Priority,
    string Status,
    string RoutingNote,
    string AssignedDate,
    string CompletedDate,
    string AssignedTo,
    bool EmailNotify
);
