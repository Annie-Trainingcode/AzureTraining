using System;

namespace EventHubShared.Models
{
    public class Mp3FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = Guid.NewGuid().ToString();
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = "audio/mpeg";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string? Artist { get; set; }
        public string? Title { get; set; }
        public string? Album { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? Bitrate { get; set; }
        public string ChecksumMd5 { get; set; } = string.Empty;
    }
}