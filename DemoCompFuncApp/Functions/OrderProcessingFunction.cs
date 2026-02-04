using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DemoCompFuncApp.Models;

namespace DemoCompFuncApp.Functions;

/// <summary>
/// HTTP Trigger Function - Order Processing API
/// Real-world scenario: E-commerce order processing endpoint
/// </summary>
public class OrderProcessingFunction
{
    private readonly ILogger _logger;

    public OrderProcessingFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrderProcessingFunction>();
    }

   [Function("OrderProcessing")]
    [QueueOutput("order-notifications", Connection = "AzureWebJobsStorage")]
    public async Task<string> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        _logger.LogInformation("=== OrderProcessing Function Started ===");

        try
        {
            // Parse the request body
            _logger.LogInformation("Reading request body...");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Request body received: {requestBody}");

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Request body is empty");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body is empty");
                /*  return new MultiResponse
                 {
                     HttpResponse = badResponse,
                     QueueMessage = null
                 }; */
                return null;
            }

            _logger.LogInformation("Deserializing order...");
            var order = JsonSerializer.Deserialize<Order>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null)
            {
                _logger.LogWarning("Failed to deserialize order");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid order data");
                /*  return new MultiResponse
                 {
                     HttpResponse = badResponse,
                     QueueMessage = null
                 }; */
                return null;
            }

            _logger.LogInformation($"Order deserialized. Customer: {order.CustomerName}, Items count: {order.Items?.Count ?? 0}");

            // Validate items
            if (order.Items == null || order.Items.Count == 0)
            {
                _logger.LogWarning("Order has no items");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Order must have at least one item");
                /*  return new MultiResponse
                 {
                     HttpResponse = badResponse,
                     QueueMessage = null
                 }; */
                return "";
            }

            // Generate order ID and set date
            order.OrderId = Guid.NewGuid().ToString();
            order.OrderDate = DateTime.UtcNow;
            order.Status = "Processing";

            // Calculate total amount
            order.TotalAmount = order.Items.Sum(item => item.Price * item.Quantity);

            _logger.LogInformation($"Order {order.OrderId} created for customer {order.CustomerName}. Total: ${order.TotalAmount}");

            // Create notification message for queue
            _logger.LogInformation("Creating queue notification...");
            var notification = new OrderNotification
            {
                OrderId = order.OrderId,
                CustomerEmail = order.CustomerEmail,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                Status = order.Status
            };

            var queueMessage = JsonSerializer.Serialize(notification);
            _logger.LogInformation($"Queue message created: {queueMessage}");

            // Create success response
            _logger.LogInformation("Creating HTTP response...");
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new
            {
                message = "Order processed successfully",
                orderId = order.OrderId,
                totalAmount = order.TotalAmount,
                status = order.Status,
                estimatedDelivery = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd")
            });

            _logger.LogInformation("=== OrderProcessing Function Completed Successfully ===");
            return queueMessage;

            /* return new MultiResponse
            {
                HttpResponse = response,
                QueueMessage = queueMessage
            }; */
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError($"JSON parsing error: {jsonEx.Message}");
            _logger.LogError($"Stack trace: {jsonEx.StackTrace}");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync($"Invalid JSON format: {jsonEx.Message}");
            // return new MultiResponse
            // {
            //     HttpResponse = errorResponse,
            //     QueueMessage = null
            // };
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing order: {ex.Message}");
            _logger.LogError($"Error type: {ex.GetType().Name}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error processing order: {ex.Message}");
            // return new MultiResponse
            // {
            //     HttpResponse = errorResponse,
            //     QueueMessage = null
            // };
            return null;
        }
    
    }

    public class MultiResponse
    {
        public HttpResponseData HttpResponse { get; set; } = null!;
        public string? QueueMessage { get; set; }
    }
}
