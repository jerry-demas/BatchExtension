namespace CBIZ.CCH.BatchExtension.Application;

public interface IRequest
{
    string url { get; }
    MultipartFormDataContent? form {get; }
    StringContent? content { get; }
    Dictionary<string, string>? headers { get; }
    CancellationToken cancellationToken { get; }
}
