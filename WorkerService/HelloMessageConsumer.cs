using System.Diagnostics;
using System.Text;
using MassTransit;
using OpenTelemetry;
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
        // Extract the PropagationContext of the upstream parent from the message headers
        // var parentContext = Propagator.Extract(default, context, ExtractTraceContext);
        //
        // // Inject extracted info into current context
        // Baggage.Current = parentContext.Baggage;
        //
        // // start an activity
         //using var activity = ActivitySource.StartActivity("message receive", ActivityKind.Consumer, parentContext.ActivityContext, tags: new[] { new KeyValuePair<string, object?>("server", Environment.MachineName) });
        //
        // AddMessagingTags(activity, context);
        //
        
        using (var activity = ActivitySource.StartActivity("SayHello"))
        {
            activity?.SetTag("foo", 1);
            activity?.SetTag("bar", "Hello, World!");
            activity?.SetTag("baz", new int[] { 1, 2, 3 });
        }
        
        _logger.LogInformation("Handling message: {message}", context.Message);

        await Task.Delay(TimeSpan.FromSeconds(5));
    }


    static void AddMessagingTags(Activity? activity, ConsumeContext receivedInfo)
    {
        // https://github.com/open-telemetry/opentelemetry-dotnet/tree/core-1.1.0/examples/MicroserviceExample/Utils/Messaging
        // Following OpenTelemetry messaging specification conventions
        // See:
        //   * https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#messaging-attributes
        //   * https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#rabbitmq

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination_kind", "queue");
        activity?.SetTag("messaging.destination", receivedInfo.DestinationAddress.ToString());
        activity?.SetTag("messaging.rabbitmq.routing_key", receivedInfo.RoutingKey);
    }

    
    IEnumerable<string> ExtractTraceContext(ConsumeContext properties, string key)
    {
        try
        {
            if (properties.Headers.TryGetHeader(key, out var value) && value is byte[] bytes)
            {
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract trace context");
        }

        return Enumerable.Empty<string>();
    }
}