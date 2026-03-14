namespace CBIZ.CCH.BatchExtension.Application;

public record GenerateRequest(
    string url, 
    MultipartFormDataContent? form = null,   
    StringContent? content = null,
    Dictionary<string, string>? headers = null,              
    CancellationToken cancellationToken = default
) : IRequest;
