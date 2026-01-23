using BuildingBlocks.Extensions;
using BuildingBlocks.Logger;
using Discount.Grpc.Data;
using Discount.Grpc.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Log services
builder.Host.UseCommonSerilog(serviceName: "Discount.Grpc");

builder.Services.AddGrpc();
builder.Services.AddDbContext<DiscountContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Database")));

// Add Swagger services
builder.Services.AddCommonSwagger("Discount API");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseMigration();
app.MapGrpcService<DiscountService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// Enable Swagger only in Development (recommended)
if (app.Environment.IsDevelopment())
{
    app.UseCommonSwagger("Discount API");
}

app.Run();