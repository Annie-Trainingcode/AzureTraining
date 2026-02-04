using EventHubShared.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace EventHubConsumer.Services
{
    public interface IMp3FileReassemblyService
    {
        Task<bool> ProcessChunkAsync(Mp3FileMessage message);
        Task<string[]> GetCompletedFileIds();
        Task<Mp3FileMetadata?> GetFileMetadataAsync(string fileId);
        Task<byte[]?> GetAssembledFileAsync(string fileId);
        Task CleanupOldFiles(TimeSpan olderThan);
    }

    public class Mp3FileReassemblyService : IMp3FileReassemblyService
    {
        private readonly ConcurrentDictionary<string, FileAssemblyInfo> _assemblyInfo = new();
        private readonly ILogger<Mp3FileReassemblyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _outputDirectory;

        public Mp3FileReassemblyService(ILogger<Mp3FileReassemblyService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _outputDirectory = _configuration.GetValue<string>("FileStorage:OutputDirectory") ?? Path.Combine(Directory.GetCurrentDirectory(), "ReceivedFiles");

            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
        }

        public async Task<bool> ProcessChunkAsync(Mp3FileMessage message)
        {
            try
            {
                var fileId = message.Metadata.FileId;

                // Get or create assembly info
                var assemblyInfo = _assemblyInfo.GetOrAdd(fileId, _ => new FileAssemblyInfo
                {
                    Metadata = message.Metadata,
                    TotalChunks = message.TotalChunks,
                    Chunks = new ConcurrentDictionary<int, byte[]>(),
                    ReceivedAt = DateTime.UtcNow
                });

                // Store the chunk
                assemblyInfo.Chunks[message.ChunkIndex] = message.FileData;

                _logger.LogDebug($"Received chunk {message.ChunkIndex + 1}/{message.TotalChunks} for file {message.Metadata.FileName}");

                // Check if all chunks are received
                if (assemblyInfo.Chunks.Count == assemblyInfo.TotalChunks)
                {
                    await AssembleAndSaveFileAsync(fileId, assemblyInfo);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing chunk for file {message.Metadata.FileId}");
                return false;
            }
        }

        public Task<string[]> GetCompletedFileIds()
        {
            var completedFiles = _assemblyInfo
                .Where(kvp => kvp.Value.IsComplete)
                .Select(kvp => kvp.Key)
                .ToArray();

            return Task.FromResult(completedFiles);
        }

        public Task<Mp3FileMetadata?> GetFileMetadataAsync(string fileId)
        {
            _assemblyInfo.TryGetValue(fileId, out var info);
            return Task.FromResult(info?.Metadata);
        }

        public async Task<byte[]?> GetAssembledFileAsync(string fileId)
        {
            if (!_assemblyInfo.TryGetValue(fileId, out var info) || !info.IsComplete)
                return null;

            var filePath = Path.Combine(_outputDirectory, $"{fileId}_{info.Metadata.FileName}");

            if (File.Exists(filePath))
            {
                return await File.ReadAllBytesAsync(filePath);
            }

            return null;
        }

        public Task CleanupOldFiles(TimeSpan olderThan)
        {
            var cutoffTime = DateTime.UtcNow - olderThan;
            var keysToRemove = new List<string>();

            foreach (var kvp in _assemblyInfo)
            {
                if (kvp.Value.ReceivedAt < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);

                    // Delete physical file if exists
                    var filePath = Path.Combine(_outputDirectory, $"{kvp.Key}_{kvp.Value.Metadata.FileName}");
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            _logger.LogInformation($"Deleted old file: {filePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete file: {filePath}");
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                _assemblyInfo.TryRemove(key, out _);
                _logger.LogInformation($"Cleaned up assembly info for file: {key}");
            }

            return Task.CompletedTask;
        }

        private async Task AssembleAndSaveFileAsync(string fileId, FileAssemblyInfo assemblyInfo)
        {
            try
            {
                // Assemble chunks in order
                var fileData = new List<byte>();
                for (int i = 0; i < assemblyInfo.TotalChunks; i++)
                {
                    if (assemblyInfo.Chunks.TryGetValue(i, out var chunk))
                    {
                        fileData.AddRange(chunk);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Missing chunk {i} for file {fileId}");
                    }
                }

                var assembledData = fileData.ToArray();

                // Verify checksum if available
                if (!string.IsNullOrEmpty(assemblyInfo.Metadata.ChecksumMd5))
                {
                    var calculatedChecksum = EventHubShared.Utils.FileUtils.CalculateMd5Hash(assembledData);
                    if (calculatedChecksum != assemblyInfo.Metadata.ChecksumMd5)
                    {
                        throw new InvalidDataException($"Checksum mismatch for file {fileId}. Expected: {assemblyInfo.Metadata.ChecksumMd5}, Calculated: {calculatedChecksum}");
                    }
                }

                // Save file
                var fileName = $"{fileId}_{assemblyInfo.Metadata.FileName}";
                var filePath = Path.Combine(_outputDirectory, fileName);

                await File.WriteAllBytesAsync(filePath, assembledData);

                assemblyInfo.IsComplete = true;
                assemblyInfo.FilePath = filePath;
                assemblyInfo.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation($"Successfully assembled and saved MP3 file: {fileName} ({assembledData.Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to assemble file {fileId}");
                throw;
            }
        }

        private class FileAssemblyInfo
        {
            public Mp3FileMetadata Metadata { get; set; } = new();
            public int TotalChunks { get; set; }
            public ConcurrentDictionary<int, byte[]> Chunks { get; set; } = new();
            public DateTime ReceivedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
            public bool IsComplete { get; set; }
            public string? FilePath { get; set; }
        }
    }
}