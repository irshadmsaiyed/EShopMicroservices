using BuildingBlocks.Logger;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Log services
builder.Host.UseCommonSerilog(serviceName: "Web");

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddTransient<LoggingDelegatingHandler>();
builder.Services.AddRefitClient<ICatalogService>()
    .ConfigureHttpClient(c => { c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!); })
    .AddHttpMessageHandler<LoggingDelegatingHandler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddRefitClient<IBasketService>()
    .ConfigureHttpClient(c => { c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!); })
    .AddHttpMessageHandler<LoggingDelegatingHandler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddRefitClient<IOrderingService>()
    .ConfigureHttpClient(c => { c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!); })
    .AddHttpMessageHandler<LoggingDelegatingHandler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

Console.WriteLine($"{builder.Configuration["ApiSettings:GatewayAddress"]}/swagger/index.html");

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri($"{builder.Configuration["ApiSettings:GatewayAddress"]}/health"), "Api Gateway",
        HealthStatus.Degraded);

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    // In this case will wait for
    //  2 ^ 1 = 2 seconds then
    //  2 ^ 2 = 4 seconds then
    //  2 ^ 3 = 8 seconds then
    //  2 ^ 4 = 16 seconds then
    //  2 ^ 5 = 32 seconds
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, retryCount, context) =>
            {
                Log.Error($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
            }
        );
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)
        );
}

var app = builder.Build();

app.UseSerilogRequestLogging();

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

app.UseAuthorization();

app.MapRazorPages();
app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

app.Run();