using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WebApiMonitoring
{
    public class PerformanceLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            await _next.Invoke(context);
            stopwatch.Stop();

            _logger.LogInformation("Request: {Method} {Path} served in {ElapsedMilliseconds}ms from {MachineName}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                System.Environment.MachineName);
        }
    }

    public static class PerformanceLoggingExtensions
    {
        public static IApplicationBuilder UsePerformanceLogging(this IApplicationBuilder builder)
            => builder.UseMiddleware<PerformanceLoggingMiddleware>();
    }
}