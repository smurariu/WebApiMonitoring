using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApiMonitoring
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Incoming request: {@Method}, {@Path}, {@Headers}",
                context.Request.Method,
                context.Request.Path,
                context.Request.Headers);

            await _next.Invoke(context);

            _logger.LogInformation("Outgoing response: {@StatusCode}, {@Headers}",
                context.Response.StatusCode,
                context.Response.Headers);
        }
    }

    public static class RequestLoggingExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder) 
            => builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}