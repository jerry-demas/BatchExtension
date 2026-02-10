namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record TrackingReportByDeliverableRequest(
    string DrawerId,
    string ServiceType,    
    // string EngagementType,
    string Workflow,
    // string CurrentStep,
    // string PIC,
    // string AssignedTo,
    // string AssignedOn,
    // string WorkflowDescription,
    // string InProcessOnly,
    // string Status,
    // string Priority,
    // string ReceivedOn,
    // string CompletedOn,
    // string SentOn,
    // string Responsible,
    // string AssignmentHistory,
    // string ReceivedFrom,
    // string SentTo,
    // string CompletedBy,
    // string Accountable,
    // string CurrentDueDate,
    // string OriginalDueDate,
    // string DateExtended,
    // string DaysAtStep,
    // string TotalDaysAtStep,
    // string DaysBetweenRoutings,
    // string TotalDaysInProcess,
    string RoutingDetails,
    // string LastUpdated,
    // List<object> InformationFields,
    List<IndexItem> Indexes
    // string PageNumber
    
   
);
