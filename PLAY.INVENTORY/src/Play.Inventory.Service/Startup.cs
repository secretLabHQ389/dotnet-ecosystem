using System;
//using System.Net.Http;
//path source commands found, might now work:
//using Polly;
//using Polly.Timeout;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service
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
            services.AddMongo()
                    .AddMongoRepository<InventoryItem>("inventoryitems")
                    .AddMongoRepository<CatalogItem>("catalogitems")
                    .AddMassTransitWithRabbitMq();

            AddCatalogClient(services);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Inventory.Service", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Inventory.Service v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static void AddCatalogClient(IServiceCollection services)
        {
            //Random jitterer = new Random() don't need Math() in C#

            services.AddHttpClient<CatalogClient>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001");
            });
            //.AddTransientHttpErrorPolicy(builder => builderOr<TimeoutRejectedException>().WaitAndRetryAsync(
            //5, //how many retries
            //retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) //how long to wait between retries
            // + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
            //onRetry: (outcome, timespan, retryAttemp) =>
            //{
            //var serviceProvider = services.BuildServiceProvider();
            //ServiceProvider.GetService<ILogger<CatalogClient>>() ? 
            //.LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");

            //}
            //))
            //.AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
            //3,
            //TimeSpan.FromSeconds(15),
            //onBreak: (outcome, timespan) =>
            //{
            //var serviceProvider = services.BuildServiceProvider();
            //ServiceProvider.GetService<ILogger<CatalogClient>>() ? 
            //.LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");

            //}
            //onReset: () =>
            // {
            //     var serviceProvider = services.BuildServiceProvider();
            //     serviceProvider.GetService<ILogger<CatalogClient>>() ?
            //         .LogWarning($"Closing the circuit...")
            // }
            //))
            //package path randomly using wrong one so add Polly not working:
            //using source flag to specify and installed.
            //.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
        }
    }
}
