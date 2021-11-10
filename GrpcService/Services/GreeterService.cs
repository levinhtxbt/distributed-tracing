using System.Diagnostics;
using Grpc.Core;

namespace GrpcService.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private static readonly ActivitySource ActivitySource = new(nameof(GreeterService));
    
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity(nameof(SayHello));
        
        // something that takes a bit
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        return new ()
        {
            Message = "Hello " + request.Name
        };
    }
}