using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using EventHubShared.Models;
using EventHubShared.Utils;
using Newtonsoft.Json;
using System.Text;

namespace EventHubSender.Services
{
    public interface IEventHubService
    {
        Task<string> SendMp3FileAsync(IFormFile file);
        Task<bool> TestConnectionAsync();
    }

    public class EventHubService : IEventHubService, IDisposable
    {
        private readonly EventHubProducerClient _producerClient;
        private readonly ILogger<EventHubService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _chunkSize;

        public EventHubService(ILogger<EventHubService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _chunkSize = _configuration.GetValue<int>("EventHub:ChunkSizeBytes", 1024 * 1024); // 1MB default

            var connectionString = _configuration.GetConnectionString("EventHub")
                ?? throw new InvalidOperationException("EventHub connection string not found");
            var eventHubName = _configuration.GetValue<string>("EventHub:Name")
                ?? throw new InvalidOperationException("EventHub name not found");

            _producerClient = new EventHubProducerClient(connectionString, eventHubName);
        }

        public async Task<string> SendMp3FileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File cannot be null or empty");

                // Read file data
                byte[] fileData;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();
                }

                // Validate MP3 file
                if (!FileUtils.IsValidMp3File(fileData))
                    throw new ArgumentException("Invalid MP3 file format");

                // Create metadata
                var metadata = new Mp3FileMetadata
                {
                    FileName = file.FileName,
                    FileSizeBytes = file.Length,
                    ContentType = file.ContentType ?? "audio/mpeg",
                    ChecksumMd5 = FileUtils.CalculateMd5Hash(fileData)
                };

                _logger.LogInformation($"Sending MP3 file: {metadata.FileName} ({metadata.FileSizeBytes} bytes)");

                // Chunk the file
                var chunks = FileUtils.ChunkFile(fileData, _chunkSize);
                var totalChunks = chunks.Count;

                // Send chunks
                for (int i = 0; i < chunks.Count; i++)
                {
                    var message = new Mp3FileMessage
                    {
                        Metadata = metadata,
                        FileData = chunks[i],
                        ChunkIndex = i,
                        TotalChunks = totalChunks,
                        IsLastChunk = i == chunks.Count - 1
                    };

                    await SendMessageAsync(message);
                    _logger.LogDebug($"Sent chunk {i + 1}/{totalChunks} for file {metadata.FileName}");
                }

                _logger.LogInformation($"Successfully sent MP3 file: {metadata.FileName} in {totalChunks} chunks");
                return metadata.FileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending MP3 file: {file?.FileName}");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var eventHubProperties = await _producerClient.GetEventHubPropertiesAsync();
                _logger.LogInformation($"Connected to EventHub: {eventHubProperties.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to EventHub");
                return false;
            }
        }

        private async Task SendMessageAsync(Mp3FileMessage message)
        {
            var json = JsonConvert.SerializeObject(message);
            var eventData = new EventData(Encoding.UTF8.GetBytes(json));

            // Add custom properties for routing and filtering
            eventData.Properties.Add("FileId", message.Metadata.FileId);
            eventData.Properties.Add("FileName", message.Metadata.FileName);
            eventData.Properties.Add("ChunkIndex", message.ChunkIndex);
            eventData.Properties.Add("TotalChunks", message.TotalChunks);
            eventData.Properties.Add("MessageType", "Mp3File");

            using var eventBatch = await _producerClient.CreateBatchAsync();

            if (!eventBatch.TryAdd(eventData))
            {
                throw new Exception($"Event is too large for the batch. Chunk size: {message.FileData.Length}");
            }

            await _producerClient.SendAsync(eventBatch);
        }

        public void Dispose()
        {
            _producerClient?.DisposeAsync().AsTask().Wait();
        }
    }
}