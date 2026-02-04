using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using EventHubShared.Models;
using Newtonsoft.Json;
using System.Text;

namespace EventHubConsumer.Services
{
    public class EventHubConsumerService : BackgroundService
    {
        private readonly ILogger<EventHubConsumerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMp3FileReassemblyService _reassemblyService;
        private EventProcessorClient? _processor;

        public EventHubConsumerService(
            ILogger<EventHubConsumerService> logger,
            IConfiguration configuration,
            IMp3FileReassemblyService reassemblyService)
        {
            _logger = logger;
            _configuration = configuration;
            _reassemblyService = reassemblyService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var eventHubConnectionString = _configuration.GetConnectionString("EventHub")
                    ?? throw new InvalidOperationException("EventHub connection string not found");
                var eventHubName = _configuration.GetValue<string>("EventHub:Name")
                    ?? throw new InvalidOperationException("EventHub name not found");
               var storageConnectionString = _configuration.GetConnectionString("StorageAccount")
                   ?? throw new InvalidOperationException("StorageAccount connection string not found");
                var blobContainerName = _configuration.GetValue<string>("EventHub:CheckpointContainer", "checkpoints");
                var consumerGroup = _configuration.GetValue<string>("EventHub:ConsumerGroup", "$Default");

             var storageClient = new BlobContainerClient(storageConnectionString, blobContainerName);
               _processor = new EventProcessorClient(storageClient, consumerGroup, eventHubConnectionString, eventHubName);

                _processor.ProcessEventAsync += ProcessEventHandler;
                _processor.ProcessErrorAsync += ProcessErrorHandler;

                await _processor.StartProcessingAsync(cancellationToken);
                _logger.LogInformation("EventHub consumer started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start EventHub consumer");
                throw;
            }

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_processor != null)
            {
                try
                {
                    await _processor.StopProcessingAsync(cancellationToken);
                    _logger.LogInformation("EventHub consumer stopped successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping EventHub consumer");
                }
            }

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Setup cleanup timer
            var cleanupInterval = _configuration.GetValue<int>("FileStorage:CleanupIntervalMinutes", 60);
            var fileRetentionHours = _configuration.GetValue<int>("FileStorage:FileRetentionHours", 24);

            using var timer = new Timer(async _ => await CleanupOldFiles(TimeSpan.FromHours(fileRetentionHours)),
                null, TimeSpan.FromMinutes(cleanupInterval), TimeSpan.FromMinutes(cleanupInterval));

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            try
            {
                if (eventArgs.Data?.Body == null)
                    return;

                var messageBody = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                var message = JsonConvert.DeserializeObject<Mp3FileMessage>(messageBody);

                if (message == null)
                {
                    _logger.LogWarning("Failed to deserialize message");
                    return;
                }

                // Validate message type
                if (eventArgs.Data.Properties.TryGetValue("MessageType", out var messageType) &&
                    messageType?.ToString() != "Mp3File")
                {
                    _logger.LogDebug($"Skipping non-MP3 message type: {messageType}");
                    return;
                }

                _logger.LogDebug($"Processing chunk {message.ChunkIndex + 1}/{message.TotalChunks} for file {message.Metadata.FileName}");

                var isComplete = await _reassemblyService.ProcessChunkAsync(message);

                if (isComplete)
                {
                    _logger.LogInformation($"MP3 file completely received and assembled: {message.Metadata.FileName} (ID: {message.Metadata.FileId})");
                }

                // Update checkpoint
                await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event");
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            _logger.LogError(eventArgs.Exception, $"Error in EventHub processing: {eventArgs.Operation}");
            return Task.CompletedTask;
        }

        private async Task CleanupOldFiles(TimeSpan retention)
        {
            try
            {
                await _reassemblyService.CleanupOldFiles(retention);
                _logger.LogInformation($"Cleaned up files older than {retention.TotalHours} hours");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }
    }
}