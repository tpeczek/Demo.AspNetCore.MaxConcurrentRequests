using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Demo.AspNetCore.MaxConcurrentRequests.Middlewares;

namespace Demo.AspNetCore.MaxConcurrentRequests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.UseMaxConcurrentRequests()
                .Run(async (context) =>
                {
                    await Task.Delay(500);

                    await context.Response.WriteAsync("-- Demo.AspNetCore.MaxConcurrentConnections --");
                });
        }
    }
}
