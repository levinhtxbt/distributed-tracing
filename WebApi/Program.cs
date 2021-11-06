using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shared.Serilog;

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

builder.Logging.AddSerilog(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddDbContext<UserDbContext>(opt => opt.UseSqlite("Data Source=user.db"));

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
app.MapGet("/signup", async (string username, UserDbContext db) =>
{
    if (!string.IsNullOrEmpty(username))
    {
        db.Users.Add(new User()
        {
            Username = username
        });
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    return Results.BadRequest();
});


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.EnsureCreated(); // good only for demos 😉
}

app.Run();

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