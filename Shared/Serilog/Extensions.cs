using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.Span;

namespace Shared.Serilog;

public static class Extensions
{
    public static ILoggingBuilder AddSerilog(this ILoggingBuilder loggingBuilder, 
        IConfiguration configuration, 
        IHostEnvironment? hostEnvironment = null)
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            // .Enrich.With<ActivityEnricher>()
            .Enrich.WithProperty("Environment", hostEnvironment?.EnvironmentName)
            .Enrich.WithProperty("ApplicationName", hostEnvironment?.ApplicationName)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .CreateLogger());

        return loggingBuilder;
    }
}