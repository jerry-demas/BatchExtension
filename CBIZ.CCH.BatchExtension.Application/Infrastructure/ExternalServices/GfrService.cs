
using Cbiz.SharedPackages;

using CBIZ.CCH.BatchExtension.Application.Features.GfrObjects;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
 using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;

internal class GfrService : IGfrService
{
    private readonly ILogger<GfrService> _logger;
    private readonly GFREndPointOptions _gfrEndPointsOptions;
    private readonly GFRApiAccessInfo _gfrApiAccessInfo;
    private readonly ProcessOptions _processOptions;
    private readonly ApiHelper _apiHelper;

    GFRAuthResponse? token = null;

    public GfrService(
        ILogger<GfrService> logger,
        IOptions<GFREndPointOptions> gfrEndPointsOptions,
        IOptions<GFRApiAccessInfo> gfrApiAccessInfo,
        IOptions<ProcessOptions> processOptions,
        ApiHelper apiHelper

    )
    {
        _logger = logger;
        _gfrEndPointsOptions = gfrEndPointsOptions.Value;
        _gfrApiAccessInfo = gfrApiAccessInfo.Value;
        _processOptions = processOptions.Value;
        _apiHelper = apiHelper;
    }

    private async Task BuildClientAuth(CancellationToken cancellationToken = default)
    {        
        if (token is null)
        {
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "X-TR-API-APP-ID", _gfrApiAccessInfo.X_TR_API_APP_ID  }
            };

            var url = $"{ApiURL(_gfrEndPointsOptions.Auth)}";

            var request = new AuthorizationRequest(_gfrApiAccessInfo.UserName, _gfrApiAccessInfo.Password);
            var jsonRequestBody = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _apiHelper.ExecutePostAsync(url, jsonRequestBody, headers, cancellationToken);
            response.Match(
                async success => { 
                    var jsonContent = await success.Content.ReadAsStringAsync();
                    token = JsonSerializer.Deserialize<GFRAuthResponse>(
                        jsonContent,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                 },
                failure => { }
            );
        }

    }

    public async Task<Possible<BatchExtensionException>> UploadDocumentToGfr(
        GfrDocument gfrDocument,
        CancellationToken cancellationToken = default)
    {
        BatchExtensionException? exception = null;

        await BuildClientAuth(cancellationToken);
        var indexResponse = await GetIndexesByDrawerIdAsync(cancellationToken);
        indexResponse.Match(
            async success =>
            {
                var newDocumentId = await CreateDocumentAsync(success, cancellationToken);
                newDocumentId.Match(
                    async success =>
                    {
                        var fileBytes = await File.ReadAllBytesAsync(gfrDocument.fileName);
                        var upoadResponse = await UploadDocumentAsync(
                             success,
                             fileBytes,
                             gfrDocument.fileName,
                             cancellationToken);

                        upoadResponse.Match(
                             success => { }, // empty intentionally
                             failure => exception = new BatchExtensionException($"Error Uploading GFR document {failure.Message}")
                        );                                 
                    },
                    failure => exception = new BatchExtensionException($"Error creating GFR document {failure.Message}")
                );
            },
            failure => exception = new BatchExtensionException($"Error getting Indexes :{failure.Message}")
        );

        if (exception != null) return exception;

        return Possible.Completed;
        
    }



    private async Task<Possible<BatchExtensionException>> UploadDocumentAsync(
        string documentId,
        byte[] documentData,
        string fileName,
        CancellationToken cancellationToken)
    {
        BatchExtensionException? exception = null;
         
        _logger.LogDebug("Uploading document: {ID}", documentId);
        var url = $"{ApiURL(_gfrEndPointsOptions.GetIndexes).Replace("{documentID}", documentId)}";

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(documentData);
        form.Add(fileContent, "file", fileName);


        var response = await _apiHelper.ExecutePostAsync(url, form, cancellationToken);
        response.Match(
            success => _logger.LogInformation($"Successfully uploaded document: {documentId}"),
            failure =>
            {
                exception = new BatchExtensionException($"Error in UploadDocumentAsync for dovumentId:{documentId}:{failure.Message}");
                _logger.LogError($"Error in UploadDocumentAsync:{failure.Message}");
            }
        );

        if (exception is not null)
            return exception;
{}              
        return Possible.Completed;

    }

    private async Task<Either<List<GetIndexResponse>, BatchExtensionException>> GetIndexesByDrawerIdAsync(CancellationToken cancellationToken = default)
    {
        BatchExtensionException? exception = null;
        List<GetIndexResponse>? result = null;

        await BuildClientAuth(cancellationToken);
        var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Basic { token }" },
                { "X-TR-API-APP-ID", _gfrApiAccessInfo.X_TR_API_APP_ID  }
            };
        var url = ApiURL(_gfrEndPointsOptions.GetIndexes).Replace("{drawerID}", _gfrApiAccessInfo.DrawerId);
        var response = await _apiHelper.ExecuteGetAsync(url, headers, cancellationToken);
        response.Match(
            async success =>
            {
                var jsonContent = await success.Content.ReadAsStringAsync();
                result = JsonSerializer.Deserialize<List<GetIndexResponse>>(
                    jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result is null || result.Count == 0)
                {
                    exception = new BatchExtensionException($"Unable to get Indexes for drawer {_gfrApiAccessInfo.DrawerId}");
                }

            },
            failure =>
                exception = new BatchExtensionException($"Unable to get Indexes for drawer {_gfrApiAccessInfo.DrawerId} : {failure.Message}")

        );

        if (result is not null)
            return result;

        if (exception is not null)
            return exception;

        return new BatchExtensionException($"Unexpected error: GetIndexesByDrawerIdAsync for drawer: {_gfrApiAccessInfo.DrawerId}");
    }

    private async Task<Either<string, BatchExtensionException>> CreateDocumentAsync(
        List<GetIndexResponse> indexes,
        CancellationToken cancellationToken = default)
    {

        BatchExtensionException? exception = null;
        CreateDocumentResponse? result = null;

        _logger.LogDebug("Creating document: {ID} for client: {Number}...", "", "");
        var url = ApiURL(_gfrEndPointsOptions.CreateDocument);

        var requestBody = new CreateDocumentRequest(_gfrApiAccessInfo.DrawerId, indexes);
        var jsonRequestBody = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _apiHelper.ExecutePostAsync(url, jsonRequestBody, null, cancellationToken);
        response.Match(
            async success =>
            {                
                var jsonContent = await success.Content.ReadAsStringAsync();
                result = JsonSerializer.Deserialize<CreateDocumentResponse>(
                    jsonContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result is null)
                {
                    exception = new BatchExtensionException($"Unable to create document");
                }
            },
            failure => exception = new BatchExtensionException($"Unable to create document : {failure.Message}")
        );


        if (result is not null)
            return result.DocumentCreate.DocumentId;

        if (exception is not null)
            return exception;

        return new BatchExtensionException($"Unexpected error: Unable to create document");

    }
    
    private string ApiURL(string api) => String.Concat(_gfrEndPointsOptions.Domain, "/", api);

}
