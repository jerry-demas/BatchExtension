using System.Net.Http.Headers;
using System.Text.Json;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;


namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.Api;

public class ApiHelper(
    IHttpClientFactory httpClientFactory,    
    ILogger<ApiHelper> logger)
{
    public const string TokenTypeBasic = "Basic";
    public const string TokenTypeBearer = "Bearer";
    public const string ContentTypeAppJson = "application/json";    
    
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ApiHelper> _logger = logger;
    
    private HttpClient CreateConfiguredClient(Dictionary<string, string>? headers)
    {
        _logger.LogInformation("In CreateConfiguredClient");
        var client = _httpClientFactory.CreateClient();

        if (headers is null) 
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
        bool isSensitiveData,      
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("In SendAsync");
        
        try
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await sendAction(client);
            
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!isSensitiveData)
            {
                _logger.LogDebug("Url:{Url} was sent. StatusCode:{StatusCode} Response:{Response}", url, response.StatusCode, responseBody);
            }

            if (!response.IsSuccessStatusCode)
            {                
                _logger.LogError("{Error}", responseBody);                       
                return new BatchExtensionException(responseBody);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending HTTP request to {Url}", url);
            return new BatchExtensionException($"Error sending HTTP request: {url}", ex);
        }
    }

    public Task<Either<HttpResponseMessage, BatchExtensionException>> ExecuteGetAsync<TRequest>(
        TRequest data)  where TRequest : IRequest =>
        SendAsync(client =>
        {
            var configuredClient = CreateConfiguredClient(data.headers);
            return configuredClient.GetAsync(data.url, data.cancellationToken);
        }, data.url, data is ISensitiveRequest, data.cancellationToken);

    public Task<Either<HttpResponseMessage, BatchExtensionException>> ExecuteFormPostAsync<TRequest>(
        TRequest data)  where TRequest : IRequest =>
        SendAsync(client =>
        {
            var configuredClient = CreateConfiguredClient(data.headers);
            return configuredClient.PostAsync(data.url, data.form, data.cancellationToken);
        }, data.url, data is ISensitiveRequest, data.cancellationToken);

    public Task<Either<HttpResponseMessage, BatchExtensionException>> ExecuteContentPostAsync<TRequest>(
        TRequest data)  where TRequest : IRequest =>
        SendAsync(client =>
        {
            var configuredClient = CreateConfiguredClient(data.headers);
            return configuredClient.PostAsync(data.url, data.content, data.cancellationToken);
        }, data.url, data is ISensitiveRequest, data.cancellationToken);
    
    public async Task<Either<T, BatchExtensionException>> DeserializeResult<T>(
        HttpResponseMessage httpResponse,
        bool isSensitive = false)
        where T : notnull
    {
        _logger.LogInformation("In DeserializeResult");
        try
        {           
            var jsonContent = await httpResponse.Content.ReadAsStringAsync();   
            if (!isSensitive) _logger.LogDebug("Deserializing response. response:{Content}", jsonContent);
            var result = JsonSerializer.Deserialize<T>(jsonContent, JsonDefaults.jsonOptions);

            if (result is null)
            {
                 _logger.LogError(
                    "Deserialization returned null. StatusCode:{StatusCode}",
                    httpResponse.StatusCode);
                return new BatchExtensionException("Error deserializing HTTP response as NULL");                
            }

            return result;
        }
        catch (Exception ex)
        {
             _logger.LogError(
                ex,
                "Exception during deserialization. StatusCode:{StatusCode}",
                httpResponse.StatusCode);
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