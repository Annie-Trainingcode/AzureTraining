using System;

namespace EventHubShared.Models
{
    public class Mp3FileMessage
    {
        public Mp3FileMetadata Metadata { get; set; } = new();
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public bool IsLastChunk { get; set; }
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}