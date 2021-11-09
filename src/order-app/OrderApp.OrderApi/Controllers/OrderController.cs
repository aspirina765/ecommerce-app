using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderApp.OrderApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IConfiguration _config;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly Random _random;

        public OrderController(ILogger<OrderController> logger, IConfiguration config, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _config = config;
            _serviceBusClient = serviceBusClient;
            _random = new Random();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder(Order order)
        {
            int processingDelay = _random.Next(0, _config.GetValue<int>("ORDER_MAX_PROCESSING_DELAY", 0));
            Thread.Sleep(processingDelay * 1000);

            var serviceBusSender = _serviceBusClient.CreateSender(_config.GetValue<string>("SERVICEBUS_TOPIC_NAME", "orders"));
            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await serviceBusSender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize<Order>(order, jsonSerializerOptions)));

            return Ok(new { message = _config.GetValue<string>("ORDER_RESPONSE_MESSAGE", "Thank you for your order!") });
        }
    }
}
