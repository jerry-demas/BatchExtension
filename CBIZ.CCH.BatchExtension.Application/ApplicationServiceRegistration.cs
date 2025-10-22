using CBIZ.CCH.BatchExtension.Application.Infrastructure.Configuration;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CBIZ.CCH.BatchExtension.Application.Infrastructure.InternalServices;
using CBIZ.CCH.BatchExtension.Application.Infrastructure;

namespace CBIZ.CCH.BatchExtension.Application;

public static class ApplicationServiceRegistration
{
    public static IHostApplicationBuilder AddApplicationServiceLayer(
           this IHostApplicationBuilder builder,
           IConfiguration configuration)
    { 
        builder.Services.Configure<DatabaseOptions>(configuration.GetSection(nameof(DatabaseOptions)));
        builder.Services.Configure<CCHEndPointOptions>(configuration.GetSection(nameof(CCHEndPointOptions)));
        builder.Services.Configure<GFREndPointOptions>(configuration.GetSection(nameof(GFREndPointOptions)));
        builder.Services.Configure<GFRApiAccessInfo>(configuration.GetSection(nameof(GFRApiAccessInfo)));
        builder.Services.Configure<ProcessOptions>(configuration.GetSection(nameof(ProcessOptions)));
        builder.Services.AddDbContext<BatchDbContext>();
        
        builder.Services.AddScoped<ICchService, CchService>();
        builder.Services.AddScoped<IGfrService, GfrService>();

        builder.Services.AddScoped<IBatchService, BatchService>();  
        builder.Services.AddScoped<IBatchRepository, BatchRepository>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        
        builder.Services.AddScoped<ApiHelper>();
    
        builder.Services.AddHttpClient("WindowsAuthClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseDefaultCredentials = true // Enables Windows Authentication (NTLM/Kerberos)
            });

        return builder;
    }
}
