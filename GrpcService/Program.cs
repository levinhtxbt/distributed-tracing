using GrpcService.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Logging.AddSerilog(builder.Configuration, builder.Environment);
builder.Services.AddGrpc();
builder.Services.AddOpenTelemetryTracing(builder =>
{
    builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GrpcService"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(nameof(GreeterService)) // when we manually create activities, we need to setup the sources here
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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();