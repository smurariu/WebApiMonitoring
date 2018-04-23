using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApi.Monitoring
{
    public class CorrelationTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public CorrelationTokenMiddleware(RequestDelegate next, ILogger<CorrelationTokenMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            Guid correlationToken;
            var headerCorrelationToken = context.Request.Headers["Correlation-Token"].FirstOrDefault();
            if (Guid.TryParse(headerCorrelationToken, out correlationToken) == false)
            {
                correlationToken = Guid.NewGuid();
            }

            context.Items["correlationToken"] = correlationToken.ToString();
            context.Response.Headers.Add("Correlation-Token", correlationToken.ToString());

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationToken"] = correlationToken }))
            {
                await _next.Invoke(context);
            }
        }
    }

    public static class CorrelationTokenMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationToken(this IApplicationBuilder builder) 
            => builder.UseMiddleware<CorrelationTokenMiddleware>();
    }
}