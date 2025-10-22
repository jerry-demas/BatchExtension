namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record CreateBatchRequest(string SubmittedBy, List<ReturnRequest> Returns)
{
    public CreateBatchRequest() : this("", new List<ReturnRequest>()) { }    
}