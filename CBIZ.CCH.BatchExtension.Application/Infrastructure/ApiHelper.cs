using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;

using Microsoft.Extensions.Logging;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure;

public class ApiHelper(
    IHttpClientFactory httpClientFactory,
    ILogger<ApiHelper> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ApiHelper> _logger = logger;

    public async Task<Either<HttpResponseMessage, BatchExtensionException>> ExecuteGetAsync(
        string url,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (var client = _httpClientFactory.CreateClient())
            {

                if (headers != null)
                {
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
                }

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    return new BatchExtensionException(
                        $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorMessage}");
                }
                return response;
            }
                        
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error in ExecutePostAsync: {url}");
            return new BatchExtensionException($"Error in ExecutePostAsync: {url}", ex);
        }
    }

    public async Task<Either<HttpResponseMessage, BatchExtensionException>> ExecutePostAsync(
        string url,
        MultipartFormDataContent form,
        CancellationToken cancellationToken = default)
    {
        try
        {
           
            using (var client = _httpClientFactory.CreateClient())
            {
                var response = await client.PostAsync(url, form, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);                   
                    return new BatchExtensionException(
                        $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorMessage}");                    
                
                }
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error in ExecutePostAsync: {url}");
            return new BatchExtensionException($"Error in ExecutePostAsync: {url}", ex);
        }
    }
    
    
    public async Task<Either<HttpResponseMessage, BatchExtensionException>> ExecutePostAsync(
        string url,
        StringContent content,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                if (headers != null)
                {
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
                }

                var response = await client.PostAsync(url, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
                    return new BatchExtensionException(
                        $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}: {errorMessage}");
                }
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error in ExecutePostAsync: {url}");
            return new BatchExtensionException($"Error in ExecutePostAsync: {url}", ex);
        }
    }
    


 

}