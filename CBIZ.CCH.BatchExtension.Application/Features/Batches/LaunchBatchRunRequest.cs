namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record LaunchBatchRunRequest(string SubmittedBy, string ReturnType, List<ReturnRequest> Returns)
{
    public LaunchBatchRunRequest() : this("", "", new List<ReturnRequest>()) { }
}




