using Serilog.Context;

namespace Bookify.WebApi.Middlewares;

public class RequestContextLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext httpContext)
    {
        using (LogContext.PushProperty("CorrelationId", GetCorrelationId(httpContext)))
        {
            return _next(httpContext);
        }
    }

    private static string GetCorrelationId(HttpContext httpContext)
    {
        httpContext.Request.Headers.TryGetValue(Constants.Context.CorrelationIdHeaderName, out var correlationId);
        
        return correlationId.FirstOrDefault() ?? httpContext.TraceIdentifier;
    }
}