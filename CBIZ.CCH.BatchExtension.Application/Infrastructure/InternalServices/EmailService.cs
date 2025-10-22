
using Cbiz.SharedPackages;
using CBIZ.CCH.BatchExtension.Application.Shared.Errors;
using Microsoft.Extensions.Logging;
using CBIZ.SharedPackages.Mail;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;

public class EmailService(
    MailService mailService,
    ILogger<EmailService> logger
) : IEmailService
{
    private readonly ILogger<EmailService> _logger = logger;
    private readonly MailService _mailService = mailService;

    public async Task<Possible<BatchExtensionException>> SendFalureEmailAsync(string message, CancellationToken cancellationToken = default)
        => await SendEmailAsync($"Failure: {message}");

    public async Task<Possible<BatchExtensionException>> SendSuccessEmailAsync(string message, CancellationToken cancellationToken = default)
        => await SendEmailAsync($"Success: {message}");


    private async Task<Possible<BatchExtensionException>> SendEmailAsync(string message, CancellationToken cancellationToken = default)
    {
       
       
        try
        {

            var mailRequest = new MailRequest(
                ToRecipients: new[] { new ToMailRecipient("jdemas@cbiz.com") },
                CcRecipients: [],
                BccRecipients: [],
                Subject: message,
                Body: message,
                AttachmentFilePaths: []
            );
            var possible = await _mailService.SendEmailAsync(mailRequest);
            await Task.Delay(1000);
            return Possible.Completed;
        }
        catch (Exception ex)
        {
            return new BatchExtensionException($"Error sending email.", ex);
        }

    }    
}
