using System.Net.Http.Headers;
using System.Text.Json;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.Extensions.Logging;


namespace CBIZ.CCH.BatchExtension.Application.Infrastructure;

public class ApiHelper(
    IHttpClientFactory httpClientFactory,    
    ILogger<ApiHelper> logger)
{
    public const string TokenTypeBasic = "Basic";
    public const string TokenTypeBearer = "Bearer";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ApiHelper> _logger = logger;
    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient CreateConfiguredClient(Dictionary<string, string>? headers)
    {
        _logger.LogInformation("In CreateConfiguredClient");
        var client = _httpClientFactory.CreateClient();

        if (headers == null) 
            return client;

        foreach (var header in headers)
        {
            if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(header.Value));
            }
            else
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return client;
    }

    private async Task<Either<HttpResponseMessage, BatchExtensionException>> SendAsync(
        Func<HttpClient, Task<HttpResponseMessage>> sendAction,
        string url,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("In SendAsync");
        
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await sendAction(client);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                return new BatchExtensionException(
                    $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorMessage}");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending HTTP request to {Url}", url);
            return new BatchExtensionException($"Error sending HTTP request: {url}", ex);
        }
    }

    public Task<Either<HttpResponseMessage, BatchExtensionException>> ExecuteGetAsync(
        string url,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(client =>
        {
            var configuredClient = CreateConfiguredClient(headers);
            return configuredClient.GetAsync(url, cancellationToken);
        }, url, cancellationToken);

    public Task<Either<HttpResponseMessage, BatchExtensionException>> ExecutePostAsync(
        string url,
        MultipartFormDataContent form,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(client => 
        {
            var configuredClient = CreateConfiguredClient(headers);
            return configuredClient.PostAsync(url, form, cancellationToken);                
        }, url, cancellationToken);
        

    public Task<Either<HttpResponseMessage, BatchExtensionException>> ExecutePostAsync(
        string url,
        StringContent content,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default) =>
        SendAsync(client =>
        {
            var configuredClient = CreateConfiguredClient(headers);
            return configuredClient.PostAsync(url, content, cancellationToken);
        }, url, cancellationToken);

    public async Task<Either<T, BatchExtensionException>> DeserializeResult<T>(HttpResponseMessage httpResponse)

        where T : notnull
    {
        _logger.LogInformation("In DeserializeResult");
        try
        {
            _logger.LogInformation("Deserializing HTTP response...");
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);

            if (result is null)
                return new BatchExtensionException("Error deserializing HTTP response");

            return result; // wraps automatically in Either<T, BatchExtensionException>
        }
        catch (Exception ex)
        {
            return new BatchExtensionException($"Error deserializing HTTP response: {ex.Message}");
        }
    }

    public static Dictionary<string, string> CreateHeader(
        string token,
        string appIdKey,
        string contentType,
        string tokenType
    )
    {       
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(token)) headers.Add("Authorization", $"{tokenType} {token}");
        if (!string.IsNullOrEmpty(appIdKey)) headers.Add("X-TR-API-APP-ID", appIdKey);                 
        if (!string.IsNullOrEmpty(contentType)) headers.Add("Content-Type", "application/json");
        
        return headers;
    }

}