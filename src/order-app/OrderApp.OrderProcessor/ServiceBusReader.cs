using Azure.Data.Tables;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderApp.OrderProcessor
{
    public class ServiceBusReader: BackgroundService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly TableClient _tableClient;
        private readonly IConfiguration _config;
        private readonly Random _random;
        
        public ServiceBusReader(IConfiguration config)
        {
            _serviceBusClient = CreateServiceBusClient(config.GetValue<string>("SERVICEBUS_CONNECTIONSTRING"), config.GetValue<string>("SERVICEBUS_AUTHENTICATION_TYPE", "CONNECTION_STRING"));
            var tableServiceClient = new TableServiceClient(config.GetValue<string>("TABLE_CONNECTIONSTRING"));
            tableServiceClient.CreateTableIfNotExists(config.GetValue<string>("TABLE_NAME", "orders"));
            _tableClient = new TableClient(config.GetValue<string>("TABLE_CONNECTIONSTRING"), config.GetValue<string>("TABLE_NAME", "orders"));
            _config = config;
            _random = new Random();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var serviceBusProcessor = _serviceBusClient.CreateProcessor(_config.GetValue<string>("SERVICEBUS_TOPIC_NAME"), _config.GetValue<string>("SERVICEBUS_SUBSCRIPTION_NAME", "order-processor")); 
            serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
            serviceBusProcessor.ProcessErrorAsync += ProcessErrorAsync;
            await serviceBusProcessor.StartProcessingAsync(stoppingToken);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs eventArgs)
        {
            Console.WriteLine($"Error while processing: {eventArgs.Exception}");
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs eventArgs)
        {
            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var order = JsonSerializer.Deserialize<Order>(eventArgs.Message.Body.ToString(), jsonSerializerOptions);
            Console.WriteLine($"Processing order with id {order.Id}.");

            int processingDelay = _random.Next(0, _config.GetValue<int>("ORDER_MAX_PROCESSING_DELAY", 0));
            Thread.Sleep(processingDelay * 1000);

            var orderEntity = new TableEntity(order.Customer, order.Id)
            {
                { "reference" , order.Reference },
                { "customer" , order.Customer },
                { "date" , order.Date },
                { "orderLines" , JsonSerializer.Serialize(order.OrderLines, typeof(List<OrderLine>)) },
            };
            await _tableClient.UpsertEntityAsync(orderEntity);

            await eventArgs.CompleteMessageAsync(eventArgs.Message, eventArgs.CancellationToken);
        }

        private ServiceBusClient CreateServiceBusClient(string connectionString, string authenticationType)
        {
            ServiceBusClient serviceBusClient;

            switch (authenticationType)
            {

                case "CONNECTION_STRING":
                    serviceBusClient =  new ServiceBusClient(connectionString);
                    break;
                case "SYSTEM_ASSIGNED_MANAGED_IDENTITY":
                    string fullyQualifiedHostName = connectionString.Split(";").Where<string>(s => s.Contains("Endpoint=sb://")).FirstOrDefault().Replace("Endpoint=sb://", "").Replace("/", "");
                    var credential = new ChainedTokenCredential(
                            new ManagedIdentityCredential(),
                            new AzureCliCredential());
                    serviceBusClient =  new ServiceBusClient(fullyQualifiedHostName, credential);
                    break;
                default:
                    serviceBusClient = new ServiceBusClient(connectionString);
                    break;
            }

            return serviceBusClient;
        }
    }
}
