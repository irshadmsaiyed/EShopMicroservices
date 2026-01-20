using BuildingBlocks.Logger;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using YarpApiGateway.Transformer;

var builder = WebApplication.CreateBuilder(args);

// Log services
// builder.Host.UseCommonSerilog(serviceName: "API.Gateway");
builder.Host.UseCommonSerilog(serviceName: "");

// Add services to the container.
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter("fixed", options =>
    {
        options.Window = TimeSpan.FromSeconds(10);
        options.PermitLimit = 5;
    });
});

builder.Services.AddSingleton<LoggingTransformProvider>();

// Correlation + outgoing call logging (shared BuildingBlocks)
//builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<LoggingDelegatingHandler>();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<LoggingTransformProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.MapReverseProxy();

app.Run();