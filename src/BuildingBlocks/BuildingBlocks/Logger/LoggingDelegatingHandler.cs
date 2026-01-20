using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Logger;

public sealed class LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;

        logger.LogInformation("Sending request {Method} {Url}", request.Method.Method, uri?.ToString() ?? "(null)");

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            logger.LogInformation("Received response {StatusCode} from {Url}", (int)response.StatusCode,
                response.RequestMessage?.RequestUri?.ToString() ?? uri?.ToString() ?? "(unknown)");

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Non-success status code {StatusCode} for {Url}",
                    (int)response.StatusCode,
                    response.RequestMessage?.RequestUri?.ToString() ?? uri?.ToString() ?? "(unknown)");
            }

            return response;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException se &&
                                              se.SocketErrorCode == SocketError.ConnectionRefused)
        {
            var hostWithPort = uri is null
                ? "(unknown host)"
                : uri.IsDefaultPort
                    ? uri.DnsSafeHost
                    : $"{uri.DnsSafeHost}:{uri.Port}";

            logger.LogCritical(ex,
                "Unable to connect to {Host}. Please check the configured service URL.",
                hostWithPort);

            return new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                RequestMessage = request
            };
        }
    }
}