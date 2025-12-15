namespace CBIZ.CCH.BatchExtension.Application.Features.Batches;

public record ReturnRequest(
    string[] FirmFlowId, 
    string ReturnId, 
    string EngagementType,
    string ClientName, 
    string ClientNumber, 
    string OfficeLocation    
    );