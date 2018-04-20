using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WebApiMonitoring
{
    public struct HealthCheck
    {
        public string DependencyName { get; }
        public bool IsDown { get; }
        public int ResponseTimeMilliseconds { get; }
        public bool IsCritical { get; }
        public string MachineName { get; set; }

        public HealthCheck(string dependencyName, bool isDown, int responseTimeMilliseconds, bool isCritical = true, string machineName = null)
        {
            DependencyName = dependencyName;
            IsDown = isDown;
            ResponseTimeMilliseconds = responseTimeMilliseconds;
            IsCritical = isCritical;
            MachineName = machineName ?? System.Environment.MachineName;
        }
    }

    public class HealthChecksMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Func<Task<HealthCheck[]>> _healthCheck;
        private readonly string _endpointPrefix;
        private readonly ILogger _logger;

        public HealthChecksMiddleware(RequestDelegate next, Func<Task<HealthCheck[]>> healthCheck, 
            string endpointPrefix, ILogger<HealthChecksMiddleware> logger)
        {
            _next = next;
            _healthCheck = healthCheck;
            _logger = logger;
            _endpointPrefix = endpointPrefix;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Equals($"/{_endpointPrefix}/ping"))
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            }
            else if (context.Request.Path.Equals($"/{_endpointPrefix}/healthCheck"))
            {
                return HealthCheck(context.Response, _healthCheck);
            }
            else
            {
                return _next.Invoke(context);
            }
        }

        private async Task HealthCheck(HttpResponse response, Func<Task<HealthCheck[]>> healthCheck)
        {
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.StatusCode = 200;

            var result = await _healthCheck?.Invoke();
            
            if (result.Any(r => r.IsCritical && r.IsDown)) 
            {
                response.StatusCode = 503;
            }

            var json = JsonConvert.SerializeObject(result);
            await response.WriteAsync(json);
        }
    }

    public static class HealthChecksExtensions
    {
        /// <summary>
        ///     Method that registers the health check middleware.
        ///     Adds two new endpoints: /_monitoring/ping and /_monitoring/healthcheck
        /// </summary>
        /// <param name="builder">The Microsoft.AspNetCore.Builder.IApplicationBuilder.</param>
        /// <param name="healthChecks">A function that performs the health checks.</param>
        /// <param name="endpointPrefix">
        ///     The prefix to use for the ping and healthcheck endpoints mentioned above.
        ///     ("_monitoring" by default)
        /// </param>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder builder,
            Func<Task<HealthCheck[]>> healthChecks, string endpointPrefix = "_monitoring") 
                => builder.UseMiddleware<HealthChecksMiddleware>(healthChecks, endpointPrefix);
    }
}