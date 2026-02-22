namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record GfrDocument(
    string ReturnId, 
    string FilingId, 
    string FileName, 
    string EngagementType,
    string DocumentType,
    string ClientName,
    string ClientNumber,
    string Description
    );
