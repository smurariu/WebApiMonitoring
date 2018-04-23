using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApi.Monitoring;

namespace WebApiApp
{
    class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddMvcCore()
                    .AddJsonFormatters();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory
                .AddDebug()
                .AddConsole();

            app.UseCorrelationToken();
            app.UseHealthChecks(() => Task.FromResult(new[]{
                new HealthCheck(dependencyName: "test dep", isDown: false, responseTimeMilliseconds: 100)}));
            
            app.UseRequestLogging();
            app.UsePerformanceLogging();
            
            app.UseMvc();
        }
    }
}
