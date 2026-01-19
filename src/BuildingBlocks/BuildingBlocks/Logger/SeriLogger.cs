using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace BuildingBlocks.Logger;

public static class SeriLogger
{
    public static IHostBuilder UseCommonSerilog(this IHostBuilder hostBuilder, string serviceName)
    {
        return hostBuilder.UseSerilog((ctx, lc) =>
            {
                var cfg = ctx.Configuration;

                var elasticUri = cfg.GetValue<string>("Elastic:Uri");
                var dsType = cfg.GetValue<string>("Elastic:DataSteam:Type") ?? "logs";
                var env = ctx.HostingEnvironment.IsDevelopment() ? "dev" : "prod";
                var app = ctx.HostingEnvironment.ApplicationName;
                var dsDataset = cfg.GetValue<string>("Elastic:DataSteam:Dataset") ?? serviceName.ToLowerInvariant();
                var dsNamespace = cfg.GetValue<string>("Elastic:DataSteam:Namespace") ?? env;

                lc.ReadFrom.Configuration(cfg)
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithProperty("Environment", env)
                    .Enrich.WithProperty("Application", app)
                    .Enrich.WithProperty("service.name", serviceName)
                    .WriteTo.Console();

                // Ship to Elasticsearch only if configured
                if (!string.IsNullOrWhiteSpace(elasticUri))
                {
                    lc.WriteTo.Elasticsearch(
                        new[] { new Uri(elasticUri) },
                        opts =>
                        {
                            // ES 8+ recommended: data streams
                            opts.DataStream = new DataStreamName(dsType, dsDataset, dsNamespace);

                            // install templates/mappings if missing
                            opts.BootstrapMethod = BootstrapMethod.Failure;
                        },
                        transport =>
                        {
                            // Auth options (enable whichever you use):

                            // Basic auth
                            var user = cfg["Elastic:UserName"];
                            var pass = cfg["Elastic:Password"];
                            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                                transport.Authentication(new BasicAuthentication(user, pass));

                            // API key
                            var apiKey = cfg["Elastic:ApiKey"];
                            if (!string.IsNullOrWhiteSpace(apiKey))
                                transport.Authentication(new ApiKey(apiKey));

                            // TLS : if using https with self-signed certificates in dev, you must trust CA properly.
                            // Avoid disabling certificate validation in production.
                        }
                    );
                }
            }
        );
    }
}