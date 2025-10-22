using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CBIZ.CCH.BatchExtension.Application.Features.Batches;


namespace CBIZ.CCH.BatchExtension.Presentation.BackgroundService;

public static class BackgroundServiceRegistration
{
    public static IHostApplicationBuilder AddBackgroundServiceLayer(
        this IHostApplicationBuilder builder,        
        IConfiguration configuration)
    {
        /*
        builder.Services.Configure<DatabaseOptions>(configuration.GetSection(nameof(DatabaseOptions)));
        builder.Services.Configure<CCHEndpointOptions>(configuration.GetSection(nameof(CCHEndpointOptions)));
        builder.Services.Configure<GFREndpointOptions>(configuration.GetSection(nameof(GFREndpointOptions)));
        */
        //builder.Services.AddScoped<ICchService, CchService>();
        //builder.Services.AddScoped<IGfrService, GfrService>();

        // builder.Services.AddDbContext<ApplicationDbContext>();
       
        return builder;
    }

}
