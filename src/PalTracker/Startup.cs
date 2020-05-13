using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Steeltoe.Management.CloudFoundry;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Hypermedia;
using Steeltoe.Management.Endpoint.Info;

namespace PalTracker
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
            services.AddCloudFoundryActuators(Configuration, MediaTypeVersion.V2, ActuatorContext.ActuatorAndCloudFoundry);
            services.AddScoped<IHealthContributor, TimeEntryHealthContributor>();
            services.AddControllers();

            var message = Configuration.GetValue<string>("WELCOME_MESSAGE","WELCOME");
            if (string.IsNullOrEmpty(message))
            {
                throw new ApplicationException("WELCOME_MESSAGE not configured.");
            }
            services.AddSingleton(sp => new WelcomeMessage(message));

            services.AddSingleton(sp => new CloudFoundryInfo(
                Configuration.GetValue<string>("PORT"),
                Configuration.GetValue<string>("MEMORY_LIMIT"),
                Configuration.GetValue<string>("CF_INSTANCE_INDEX"),
                Configuration.GetValue<string>("CF_INSTANCE_ADDR")
            ));
            services.AddSingleton<IInfoContributor, TimeEntryInfoContributor>();
            services.AddDbContext<TimeEntryContext>(options => options.UseMySql(Configuration));

            //services.AddSingleton<ITimeEntryRepository, InMemoryTimeEntryRepository>();
            services.AddScoped<ITimeEntryRepository, MySqlTimeEntryRepository>();
            services.AddSingleton<IOperationCounter<TimeEntry>, OperationCounter<TimeEntry>>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            app.UseCloudFoundryActuators(MediaTypeVersion.V2, ActuatorContext.ActuatorAndCloudFoundry);
        }
    }
}
