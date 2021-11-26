using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OrderApp.OrderApi
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
            var credential = new ChainedTokenCredential(
                        new ManagedIdentityCredential(),
                        new AzureCliCredential());


            services.AddAzureClients(builder => {
                //Inject Service Bus Client with/without managed identity credentials
                switch (Configuration.GetValue<String>("SERVICEBUS_AUTHENTICATION_TYPE", "CONNECTION_STRING")) 
                {
                    case "CONNECTION_STRING":
                        builder.AddServiceBusClient(Configuration.GetValue<string>("SERVICEBUS_CONNECTIONSTRING"));
                        break;
                    case "SYSTEM_ASSIGNED_MANAGED_IDENTITY":
                        builder.AddServiceBusClientWithNamespace(GetFullyQualifiedHostName(Configuration.GetValue<string>("SERVICEBUS_CONNECTIONSTRING"))).WithCredential(credential);
                        break;
                }

                //Inject Blob Service Client with/without managed identity credentials
                switch (Configuration.GetValue<string>("BLOB_AUTHENTICATION_TYPE", "CONNECTION_STRING"))
                {
                    case "CONNECTION_STRING":
                        builder.AddBlobServiceClient(Configuration.GetValue<string>("BLOB_CONNECTIONSTRING"));
                        break;
                    case "SYSTEM_ASSIGNED_MANAGED_IDENTITY":
                        builder.AddBlobServiceClient(GetBlobStorageUri(Configuration.GetValue<string>("BLOB_CONNECTIONSTRING"))).WithCredential(credential);
                        break;
                }
            });

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private Uri GetBlobStorageUri(string blobConnectionString)
        {
            string protocol = blobConnectionString.Split(";").Where<string>(s => s.Contains("DefaultEndpointsProtocol=")).FirstOrDefault().Replace("DefaultEndpointsProtocol=", "");
            string accountName = blobConnectionString.Split(";").Where<string>(s => s.Contains("AccountName=")).FirstOrDefault().Replace("AccountName=", "");
            string suffix = blobConnectionString.Split(";").Where<string>(s => s.Contains("EndpointSuffix=")).FirstOrDefault().Replace("EndpointSuffix=", "");
            return new Uri($"{protocol}://{accountName}.blob.{suffix}");
        }

        private string GetFullyQualifiedHostName(string serviceBusConnectionString)
        {
            return serviceBusConnectionString.Split(";").Where<string>(s => s.Contains("Endpoint=sb://")).FirstOrDefault().Replace("Endpoint=sb://", "").Replace("/", "");
        }
    }
}
