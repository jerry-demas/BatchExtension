namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record RouteWorkflowRequest
(
    List<string> FilingId,
    List<string> CurrentStep,
    bool Complete,
    string CompletedDate,
    string NextStep,
    string AssignedTo,
    string AssignedDate,
    string Priority,
    string Status,
    string RoutingNote,
    string EmailNotify
);
