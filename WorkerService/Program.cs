using WorkerService;
using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Serilog;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddOpenTelemetryTracing(builder =>
        {
            builder
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WorkerService"))
                .AddMassTransitInstrumentation()
                .AddSource(nameof(HelloMessageConsumer)) // when we manually create activities, we need to setup the sources here
                .AddZipkinExporter(options =>
                {
                    // not needed, it's the default
                    options.Endpoint = new Uri("http://msi.local:9411/api/v2/spans");
                })
                .AddJaegerExporter(options =>
                {
                    // not needed, it's the default
                    options.AgentHost = "msi.local";
                    options.AgentPort = 6831;
                });
        });
        services.AddMassTransit(c =>
        {
            c.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host("msi.local", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                configurator.ConfigureEndpoints(context);
            });

            c.AddConsumer<HelloMessageConsumer>();
           
        });
        services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
        services.AddMassTransitHostedService();
    })
    .ConfigureLogging((context, builder) =>
    {
        builder.AddSerilog(context.Configuration, context.HostingEnvironment);
    })
    .Build();

await host.RunAsync();