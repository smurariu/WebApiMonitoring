using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog.Context;

namespace WebApiService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<CorrelationTokenMiddleware>();
            //app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<PerformanceLoggingMiddleware>();

            Func<Task<HealthCheck[]>> healthCheck = HealthChecker;

            app.UseMiddleware<MonitoringMiddleware>(healthCheck);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }

        Task<HealthCheck[]> HealthChecker()
        {
            HealthCheck[] result = new HealthCheck[1];
            result[0] = new HealthCheck("ElasticSearch", false, new Random().Next(100,500));
            return Task.FromResult(result);
        }
    }

    public class CorrelationTokenMiddleware
    {
        private RequestDelegate _next;

        public CorrelationTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Guid correlationToken;
            if (!(context.Request.Headers["Correlation-Token"].FirstOrDefault() != null && Guid.TryParse(context.Request.Headers["Correlation-Token"],
            out correlationToken))) correlationToken = Guid.NewGuid();

            context.Items["correlationToken"] = correlationToken.ToString(); //inject this later
            context.Response.Headers.Add("Correlation-Token", correlationToken.ToString());
            using (LogContext.PushProperty("CorrelationToken", correlationToken))
                await _next.Invoke(context);
        }
    }

    public class RequestLoggingMiddleware
    {
        private RequestDelegate _next;
        private Serilog.ILogger _logger;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Serilog.Log.Logger;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.Information("Incoming request: {@Method}, {@Path}, {@Headers}",
                context.Request.Method,
                context.Request.Path,
                context.Request.Headers);

            await _next.Invoke(context);

            _logger.Information("Outgoing response: {@StatusCode}, {@Headers}",
                context.Response.StatusCode,
                context.Response.Headers);
        }
    }

    public class PerformanceLoggingMiddleware
    {
        private RequestDelegate _next;
        private Serilog.ILogger _logger;

        public PerformanceLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Serilog.Log.Logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            await _next.Invoke(context);
            stopwatch.Stop();

            _logger.Information("Request: {@Method} {@Path} served in {ElapsedMilliseconds}ms from {MachineName}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                System.Environment.MachineName);
        }
    }

    public class MonitoringMiddleware
    {
        private RequestDelegate _next;
        private readonly Func<Task<HealthCheck[]>> _healthCheck;
        private Serilog.ILogger _logger;

        public MonitoringMiddleware(RequestDelegate next, Func<Task<HealthCheck[]>> healthCheck)
        {
            _next = next;
            _healthCheck = healthCheck;
            _logger = Serilog.Log.Logger;
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Equals("/_monitoring/ping"))
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            }
            else if (context.Request.Path.Equals("/_monitoring/healthCheck"))
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
            var result = await _healthCheck();
            
            if (result.Any(r => r.IsCritical && r.IsDown)) response.StatusCode = 503;
            else response.StatusCode = 200;
            
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var json = JsonConvert.SerializeObject(result);
            await response.WriteAsync(json);
        }
    }
}
