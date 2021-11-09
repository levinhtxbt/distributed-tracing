
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Serilog;
using WebApp.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSerilog(builder.Configuration, builder.Environment);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddHttpClient("WebApi", c =>
{
    c.BaseAddress = new Uri("https://localhost:5001");
});

builder.Services.AddOpenTelemetryTracing(builder =>
{
    builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApp"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // to avoid double activity, one for HttpClient, another for the gRPC client
        // -> https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.1.0/src/OpenTelemetry.Instrumentation.GrpcNetClient/README.md#suppressdownstreaminstrumentation
        .AddGrpcClientInstrumentation(options => options.SuppressDownstreamInstrumentation = true)
        // besides instrumenting EF, we also want the queries to be part of the telemetry (hence SetDbStatementForText = true)
        .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
        //.AddSource(nameof(MessagePublisher)) // when we manually create activities, we need to setup the sources here
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");



app.Run();