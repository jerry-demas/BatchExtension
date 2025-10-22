using Microsoft.IdentityModel.Tokens;
using System.Reflection;
class Program{
    static void Main()
    {
    
 }

}

/*

using CBIZ.CCH.BatchExtension.Application;
using CBIZ.CCH.BatchExtension.Application.Features.Blurbs;
using CBIZ.CCH.BatchExtension.Application.Features.Dice;
using CBIZ.CCH.BatchExtension.Presentation.BackgroundService;
using CBIZ.CCH.BatchExtension.Presentation.BackgroundService.Infrastructure.Persistance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddBlurbService();
//builder.Services.AddDiceService();

builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddNLog(new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog")));
    });

builder.AddBackgroundServiceLayer(builder.Configuration)
       .AddApplicationServiceLayer(builder.Configuration);

builder.Configuration.AddUserSecrets<ApplicationDbContext>();

builder.Services.AddHostedService<Worker>(); //to convert this to a windows service: https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service 


using IHost host = builder.Build();

await host.RunAsync();

*/