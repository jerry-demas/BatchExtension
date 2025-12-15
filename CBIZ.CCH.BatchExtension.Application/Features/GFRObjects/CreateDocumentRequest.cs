namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;


public record CreateDocumentRequest
{
    public string DrawerId { get; init; }
    public List<IndexItem> Indexes { get; init; }

    public CreateDocumentRequest(        
        string drawerId, 
        GfrDocument document,
        List<GetIndexResponse> indexes)
    {
        DrawerId = drawerId;
        Indexes = GetIndexResponse.BuildFromRequest(indexes, document);
    }
}
