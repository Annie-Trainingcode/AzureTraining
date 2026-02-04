/*  using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DemoCompFuncApp.Models;
using System.Text.Json;

namespace DemoCompFuncApp.Functions;

/// <summary>
/// Queue Trigger - Order Notification Processor
/// Simple demonstration: Automatically processes messages from queue
/// </summary>
public class OrderNotificationProcessorFunction
{
    private readonly ILogger _logger;

    public OrderNotificationProcessorFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderNotificationProcessorFunction>();
    }

    [Function("OrderNotificationProcessor")]
    public async Task Run(
        [QueueTrigger("order-notifications", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation($"Processing order notification from queue");

        if (string.IsNullOrEmpty(queueMessage))
        {
            _logger.LogWarning("Empty queue message received");
            return;
        }

        var notification = JsonSerializer.Deserialize<OrderNotification>(queueMessage);

        _logger.LogInformation($"Order notification retrieved: OrderId={notification?.OrderId}, Customer={notification?.CustomerName}");
        
        // Display the values
        _logger.LogInformation($"Order Details: {JsonSerializer.Serialize(notification, new JsonSerializerOptions { WriteIndented = true })}");

        await Task.CompletedTask;
    }
}  */