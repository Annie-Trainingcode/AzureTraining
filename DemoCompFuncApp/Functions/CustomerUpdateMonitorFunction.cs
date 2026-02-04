using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Extensions.Logging;
using DemoCompFuncApp.Models;
using System.Text.Json;
using Azure.Storage.Blobs;

namespace DemoCompFuncApp.Functions;

/// <summary>
/// SQL Trigger Function - Customer Update Monitor
/// Real-world scenario: Monitor customer table changes for audit/sync purposes
/// Triggers when a customer record is inserted or updated in SQL database
/// Useful for: audit logging, data synchronization, cache invalidation, notifications
/// </summary>
public class CustomerUpdateMonitorFunction
{
    private readonly ILogger _logger;
    private readonly BlobContainerClient _blobContainerClient;

    public CustomerUpdateMonitorFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CustomerUpdateMonitorFunction>();
        
        // Initialize blob client
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var blobServiceClient = new BlobServiceClient(connectionString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient("customer-audit");
        _blobContainerClient.CreateIfNotExists();
    }

    [Function("CustomerUpdateMonitor")]
    public async Task Run(
        [SqlTrigger("[dbo].[Customers]", "SqlConnectionString")] 
        IReadOnlyList<SqlChange<Customer>> changes)
    {
        _logger.LogInformation($"SQL Trigger detected {changes.Count} change(s) in Customers table");

        var auditEntries = new List<object>();

        foreach (var change in changes)
        {
            var customer = change.Item;
            
            _logger.LogInformation($"Customer change detected: ID={customer.CustomerId}, Name={customer.CustomerName}, Email={customer.Email}");

            var auditEntry = new
            {
                ChangeId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                ChangeType = "Update",
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                Status = customer.Status,
                LastUpdated = customer.LastUpdated,
                Details = new
                {
                    Message = $"Customer {customer.CustomerName} (ID: {customer.CustomerId}) was updated",
                    PreviousStatus = "Active",
                    NewStatus = customer.Status,
                    UpdatedFields = new[] { "Email", "PhoneNumber", "Status" }
                }
            };

            auditEntries.Add(auditEntry);

            // Additional business logic based on customer changes
            if (customer.Status == "Inactive")
            {
                _logger.LogWarning($"Customer {customer.CustomerName} (ID: {customer.CustomerId}) has been deactivated");
            }
            else if (customer.Status == "VIP")
            {
                _logger.LogInformation($"Customer {customer.CustomerName} (ID: {customer.CustomerId}) upgraded to VIP status");
            }
        }

        // Create audit log
        var auditLog = new
        {
            AuditDate = DateTime.UtcNow,
            TotalChanges = changes.Count,
            Changes = auditEntries
        };

        var auditJson = JsonSerializer.Serialize(auditLog, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Upload to blob storage directly
        var blobName = $"customer-changes-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(auditJson)))
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        _logger.LogInformation($"Audit log created with {auditEntries.Count} entries and saved to blob: {blobName}");
    }
}