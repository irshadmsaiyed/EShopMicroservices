using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace YarpApiGateway.Transformer;

public sealed class LoggingTransformProvider(ILogger<LoggingTransformProvider> logger) : ITransformProvider
{
    private const string CorrelationHeader = "X-Correlation-ID";

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(requestContext =>
        {
            var http = requestContext.HttpContext;

            // 1.Get or create CorrelationId
            var correlationId =
                http.Request.Headers.TryGetValue(CorrelationHeader, out var cid) &&
                !string.IsNullOrWhiteSpace(cid)
                    ? cid.ToString()
                    : Guid.NewGuid().ToString("N");

            // 2.Store it for response phase
            http.Items[CorrelationHeader] = correlationId;

            // 3.Add to outgoing proxied request
            requestContext.ProxyRequest.Headers.Remove(CorrelationHeader);
            requestContext.ProxyRequest.Headers.TryAddWithoutValidation(CorrelationHeader, correlationId);

            logger.LogInformation(
                "Sending request {Method} {Path} (Route: {RouteId}, Cluster: {ClusterId}, CorrelationId: {CorrelationId})",
                http.Request.Method,
                http.Request.Path.Value ?? "/",
                context.Route.RouteId,
                context.Cluster.ClusterId,
                correlationId);

            return ValueTask.CompletedTask;
        });

        context.AddResponseTransform(responseContext =>
        {
            var http = responseContext.HttpContext;

            var correlationId = http.Items.TryGetValue(CorrelationHeader, out var cidObj)
                ? cidObj?.ToString()
                : null;

            // 4.Return CorrelationId to client
            if (!string.IsNullOrWhiteSpace(correlationId))
                http.Response.Headers[CorrelationHeader] = correlationId;

            var statusCode = responseContext.ProxyResponse?.StatusCode;
            var statusCodeValue = statusCode.HasValue ? (int)statusCode.Value : 0;

            if (statusCodeValue == 0)
            {
                // Network failure / connection refused / timeout
                logger.LogCritical(
                    "Proxy failure {Method} {Path} (Route: {RouteId}, Cluster: {ClusterId}, CorrelationId: {CorrelationId})",
                    http.Request.Method,
                    http.Request.Path.Value ?? "/",
                    context.Route.RouteId,
                    context.Cluster.ClusterId,
                    correlationId
                );

                return ValueTask.CompletedTask;
            }

            logger.LogInformation(
                "Received response {StatusCode} for {Method} {Path} (Route:{RouteId}, Cluster: {ClusterId}, CorrelationId: {CorrelationId})",
                statusCode,
                http.Request.Method,
                http.Request.Path.Value ?? "/",
                context.Route.RouteId,
                context.Cluster.ClusterId,
                correlationId
            );

            if (statusCodeValue < 200 || statusCodeValue >= 300)
            {
                logger.LogWarning(
                    "Non-success status code {StatusCode} for {Method} {Path} (Route: {RouteId}, Cluster: {ClusterId}, CorrelationId: {CorrelationId})",
                    statusCodeValue,
                    http.Request.Method,
                    http.Request.Path.Value ?? "/",
                    context.Route.RouteId,
                    context.Cluster.ClusterId,
                    correlationId);
            }

            return ValueTask.CompletedTask;
        });
    }
}