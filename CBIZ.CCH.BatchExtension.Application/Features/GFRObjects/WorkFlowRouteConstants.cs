namespace CBIZ.CCH.BatchExtension.Application.Features.GFRObjects;

public static class WorkFlowRouteConstants
{
    public const string DocumentType = "EXTENSION";
    public const string ClientNumberIndex = "0000000002";
    public const string TaxYearIndex = "0000000006";    
    public const string Action = "EXTEND";
    public const string NextStepProcessing = "PROCESSING";
    public const string NextStepError = "AUTOMATION ERROR";
    public const string Priority = "MEDIUM";
    public const string Status = "RECURRING";
    public const string RoutingNote = "Routed via Batch Extension";    
        
    public record RouteAssignedTo(string AssignedToName, string AssignedToCode)
    {
        public static readonly RouteAssignedTo AssignedToGood = new("P-NATL EXT BATCH PROCESSING", "G");
        public static readonly RouteAssignedTo AssignedToFailed = new("P-AUTOMATION ERROR", "G");

         public static IEnumerable<RouteAssignedTo> List() =>
            new[] { AssignedToGood, AssignedToFailed };


    }

}
