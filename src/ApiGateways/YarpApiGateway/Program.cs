using BuildingBlocks.Extensions;
using BuildingBlocks.Logger;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using YarpApiGateway.Transformer;

var builder = WebApplication.CreateBuilder(args);

// Log services
builder.Host.UseCommonSerilog(serviceName: "Api.Gateway");

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
//builder.Services.AddTransient<LoggingDelegatingHandler>();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<LoggingTransformProvider>();

// Add Swagger services
builder.Services.AddCommonSwagger("API Gateway");

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.MapReverseProxy();

// Enable Swagger only in Development (recommended)
if (app.Environment.IsDevelopment())
{
    app.UseCommonSwagger("Basket API");
}

app.MapHealthChecks("/health");

app.Run();