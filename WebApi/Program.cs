
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Configure Service  
builder.Services.AddCors(options =>
{
    options.AddPolicy("custom", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.AddControllers();
builder.Services.AddDbContext<UserDbContext>(opt => opt.UseSqlite("Data Source=user.db"));
builder.Logging.AddSerilog(builder.Configuration, builder.Environment);
builder.Services.AddOpenTelemetryTracing(builder =>
{
    builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMassTransitInstrumentation()
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
builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri("http://localhost:5050");
});
builder.Services.AddMassTransit(c =>
{
    c.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host("msi.local", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("custom");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/signup", async (
    [FromQuery] string username, 
    [FromServices] UserDbContext db,
    [FromServices] Greeter.GreeterClient protoService,
    [FromServices] IPublishEndpoint publishEndpoint ) =>
{
    if (!string.IsNullOrEmpty(username))
    {
        db.Users.Add(new User()
        {
            Username = username
        });
        await db.SaveChangesAsync();
        
        var greetingMessage = await protoService.SayHelloAsync(new HelloRequest()
        {
            Name = username
        });
        
        await publishEndpoint.Publish(new HelloMessage(greetingMessage.Message));
        
        return Results.Ok(new SignupResponse(greetingMessage.Message));
    }

    return Results.BadRequest();
});


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.EnsureCreated(); // good only for demos 😉
}

app.Run();

public record struct SignupResponse(string Message);

public class UserDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(x => x.Id);
    }
}

public class User
{
    public int Id { get; set; }

    public string Username { get; set; }
}