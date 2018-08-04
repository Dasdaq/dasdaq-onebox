using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Dasdaq.Dev.Agent.Models;
using Dasdaq.Dev.Agent.Services;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

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
            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dasdaq"));
            app.UseMvcWithDefaultRoute();
            app.UseVueMiddleware();
            app.UseDeveloperExceptionPage();
            this.PreinstallInstances(app.ApplicationServices);
        }

        private void PreinstallInstances(IServiceProvider services)
        {
            var instances = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json")).Instances;
            Task.Run(() => {
                using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var ins = serviceScope.ServiceProvider.GetService<InstanceService>())
                {
                    if (instances == null)
                    {
                        return;
                    }
                    foreach (var x in instances)
                    {
                        ins.DownloadAndStartInstanceAsync(
                            Path.GetFileNameWithoutExtension(x), 
                            InstanceUploadMethod.Git, 
                            x);
                    }
                }
            });
        }
    }
}
