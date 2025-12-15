
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.Extensions.Logging;
using CBIZ.SharedPackages.Mail;

using CBIZ.CCH.BatchExtension.Application.Features.Batches;

using System.Text;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.EmailTemplates;
using System.Net.Quic;
using System.Xml.Linq;
using System.Text.Json;
using CBIZ.CCH.BatchExtension.Application.Features.Batches.BatchQueueObjects;


namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;

public class EmailService: IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly MailService _mailService;

    public EmailService(
        ILogger<EmailService> logger,
        MailService mailService
        )
    {
        _logger = logger;
        _mailService = mailService;
    }

    public async Task<Possible<BatchExtensionException>> SendEmailFailureAsync(string submittedBy, string message, CancellationToken cancellationToken = default)
        => await SendEmailAsync(submittedBy, $"Failure: {message}", cancellationToken);

    public async Task<Possible<BatchExtensionException>> SendEmailSuccessAsync(string submittedBy, string message, CancellationToken cancellationToken = default)
        => await SendEmailAsync(submittedBy, $"Success: {message}", cancellationToken);

    public async Task<Possible<BatchExtensionException>> SendEmailSuccessBatchCreatedAsync(
        LaunchBatchRunRequest request,
        Guid queueId,
        CancellationToken cancellationToken = default)
            => await SendEmailAsync(request.SubmittedBy, BatchCreationEmailBody(request, queueId), cancellationToken);

    public async Task<Possible<BatchExtensionException>> SendEmailSuccessfullQueueProcessAsync(
        string submittedBy,
        List<BatchQueueStatusResponse> status,
        CancellationToken cancellationToken = default)  
            => await SendEmailAsync(submittedBy, BatchStatusEmailBody(HtmlBuilder.TextValueColorGreen("Batch ran successfully"), status), cancellationToken);
    
    
    public async Task<Possible<BatchExtensionException>> SendEmailFailedQueueProcessAsync(
        string submittedBy,
        List<BatchQueueStatusResponse> status,
        CancellationToken cancellationToken = default) 
            => await SendEmailAsync(submittedBy, BatchStatusEmailBody(HtmlBuilder.TextValueColorRed("Batch ran with errors"), status), cancellationToken);
    

    private async Task<Possible<BatchExtensionException>> SendEmailAsync(
        string toAddress,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending email");

            var mailRequest = new MailRequest(
                ToRecipients: new[] { new ToMailRecipient(toAddress) },
                CcRecipients: [],
                BccRecipients: [],
                Subject: "BatchExtension",
                Body: message,
                AttachmentFilePaths: []
            );
            var possible = await _mailService.SendEmailAsync(mailRequest, cancellationToken);            
            if(possible.HasFailure)
            {
                return new BatchExtensionException($"Error sending email.", possible.Failure);
            }        
            
            return Possible.Completed;
        }
        catch (Exception ex)
        {
            return new BatchExtensionException($"Error sending email.", ex);
        }

    }


    private static string BatchCreationEmailBody(
        LaunchBatchRunRequest request,
        Guid queueId)
    {
        StringBuilder htmlBody = new StringBuilder();

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(request));
        var returns = doc.RootElement.GetProperty("Returns");
        var tableLists = HtmlBuilder.GetTableData(returns);

        htmlBody.AppendLine(HtmlBuilder.Header());
        htmlBody.AppendLine($"Batch queueId:<B>{queueId}</B></br>");        
        htmlBody.AppendLine("<Table>");
        htmlBody.AppendLine(HtmlBuilder.TableHeader(tableLists.headers));
        htmlBody.AppendLine(HtmlBuilder.TableRow(tableLists.dataRows));
        htmlBody.AppendLine("</Table>");
        return $"<!doctype html><html>{htmlBody}</html>";
        
    }

    private static string BatchStatusEmailBody(
        string message,
        List<BatchQueueStatusResponse> status
    )
    {
        StringBuilder htmlBody = new StringBuilder();             
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(status[0]));        
        var batchItemsList =  JsonDocument.Parse(JsonSerializer.Serialize(status[0].BatchItems.ConvertForEmailList()));
        var items = batchItemsList.RootElement;
        var tableLists = HtmlBuilder.GetTableData(items);

        htmlBody.AppendLine(HtmlBuilder.Header());
        htmlBody.AppendLine($"<B>{message}</B><br>");
        htmlBody.AppendLine($"Batch queueId:<B>{doc.RootElement.GetProperty("QueueId")}</B><br>");        
        htmlBody.AppendLine($"Status:<B>{doc.RootElement.GetProperty("QueueStatus")}</B><br>");
        htmlBody.AppendLine("<Table>");
        htmlBody.AppendLine(HtmlBuilder.TableHeader(tableLists.headers));
        htmlBody.AppendLine(HtmlBuilder.TableRow(tableLists.dataRows));
        htmlBody.AppendLine("</Table>");
        return $"<!doctype html><html>{htmlBody}</html>";
    }
}
