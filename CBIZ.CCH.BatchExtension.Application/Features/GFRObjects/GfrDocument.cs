namespace CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;

public record GfrDocument(
    string returnId, 
    string filingId, 
    string fileName, 
    string engagementType,
    string documentType,
    string clientName,
    string clientNumber
    );
