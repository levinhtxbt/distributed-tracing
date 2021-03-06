using System;
using MassTransit;
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
builder.Services.AddHttpClient("WebApi", c => { c.BaseAddress = new Uri("https://localhost:5001"); });

builder.Services.AddOpenTelemetryTracing(builder =>
{
    builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApp"))
        .AddAspNetCoreInstrumentation(
            // if we wanted to ignore some specific requests, we could use the filter
            options => options.Filter = httpContext =>
                !httpContext.Request.Path.Value?.Contains("/_framework/aspnetcore-browser-refresh.js") ?? true)
        .AddHttpClientInstrumentation( // we can hook into existing activities and customize them
            options => options.Enrich = (activity, eventName, rawObject) =>
            {
                if (eventName == "OnStartActivity" && rawObject is System.Net.Http.HttpRequestMessage request &&
                    request.Method == HttpMethod.Get)
                {
                    activity.SetTag("RandomDemoTag", "Adding some random demo tag, just to see things working");
                }
            })
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