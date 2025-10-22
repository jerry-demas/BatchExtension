namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;


public record CreateDocumentRequest(string drawerId, List<GetIndexResponse> indexes);
