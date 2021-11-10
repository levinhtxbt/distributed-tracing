using System.Diagnostics;
using MassTransit;
using OpenTelemetry.Context.Propagation;
using Shared;

namespace WorkerService;

public class HelloMessageConsumer : IConsumer<HelloMessage>
{
    private readonly ILogger<HelloMessageConsumer> _logger;
    private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(HelloMessageConsumer));
    private static readonly TextMapPropagator Propagator = new TraceContextPropagator();
    
    public HelloMessageConsumer(ILogger<HelloMessageConsumer> logger)
    {
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<HelloMessage> context)
    {
        using (var activity = ActivitySource.StartActivity("SayHello"))
        {
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });
            
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        
        _logger.LogInformation("Handling message: {message}", context.Message);

        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}