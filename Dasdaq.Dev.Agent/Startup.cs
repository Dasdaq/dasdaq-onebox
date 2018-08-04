using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Dasdaq.Dev.Agent.Models;
using Dasdaq.Dev.Agent.Services;

namespace Dasdaq.Dev.Agent
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddMemoryCache()
                .AddDbContext<AgentContext>(x => x.UseInMemoryDatabase());
            services.AddAgentServices();
        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseErrorHandlingMiddleware();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
            app.UseVueMiddleware();
            app.UseDeveloperExceptionPage();
        }
    }
}
