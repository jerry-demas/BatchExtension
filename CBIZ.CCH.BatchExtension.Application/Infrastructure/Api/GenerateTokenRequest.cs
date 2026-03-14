namespace CBIZ.CCH.BatchExtension.Application;


public record GenerateTokenRequest(
    string url,
    MultipartFormDataContent? form = null,
    StringContent? content = null,
    Dictionary<string, string>? headers = null,              
    CancellationToken cancellationToken = default
) : ISensitiveRequest;