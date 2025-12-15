using NLog.Extensions.Logging;

namespace CBIZ.CCH.BatchExtension.API;

public static class ProgramServiceRegistration
{
    public static IHostApplicationBuilder AddProgramServiceLayer(
            this IHostApplicationBuilder builder,
            IConfiguration configuration)
    {
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog(new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog")));
        });
        return builder;
    }
}
