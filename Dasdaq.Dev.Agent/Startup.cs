using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Dasdaq.Dev.Agent.Models;
using Dasdaq.Dev.Agent.Services;
using Swashbuckle.AspNetCore.Swagger;
using Dasdaq.Dev.Agent.Hubs;

namespace Dasdaq.Dev.Agent
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
            services.AddMvc();
            services.AddMemoryCache()
                .AddDbContext<AgentContext>(x => x.UseInMemoryDatabase());
            services.AddAgentServices();
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new Info() { Title = "Dasdaq", Version = "v1" });
                x.DocInclusionPredicate((docName, apiDesc) => apiDesc.HttpMethod != null);
                x.DescribeAllEnumsAsStrings();
            });
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseErrorHandlingMiddleware();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseSignalR(x =>
            {
                x.MapHub<AgentHub>("/signalr/agent");
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dasdaq"));
            app.UseMvcWithDefaultRoute();
            app.UseVueMiddleware();
            app.UseDeveloperExceptionPage();
        }
    }
}
