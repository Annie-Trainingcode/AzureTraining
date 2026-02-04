using EventHubSender.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventHubSender.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Mp3FilesController : ControllerBase
    {
        private readonly IEventHubService _eventHubService;
        private readonly ILogger<Mp3FilesController> _logger;

        public Mp3FilesController(IEventHubService eventHubService, ILogger<Mp3FilesController> logger)
        {
            _eventHubService = eventHubService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
        public async Task<IActionResult> UploadMp3File(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                if (!file.ContentType.StartsWith("audio/") && !Path.GetExtension(file.FileName).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "Only MP3 files are allowed" });
                }

                if (file.Length > 100 * 1024 * 1024) // 100MB
                {
                    return BadRequest(new { error = "File size exceeds 100MB limit" });
                }

                var fileId = await _eventHubService.SendMp3FileAsync(file);

                return Ok(new
                {
                    message = "MP3 file uploaded and sent to EventHub successfully",
                    fileId = fileId,
                    fileName = file.FileName,
                    fileSize = file.Length,
                    uploadedAt = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid file upload attempt");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading MP3 file");
                return StatusCode(500, new { error = "Internal server error occurred while uploading file" });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var isConnected = await _eventHubService.TestConnectionAsync();

                if (isConnected)
                {
                    return Ok(new
                    {
                        status = "healthy",
                        message = "EventHub connection is working",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        message = "Unable to connect to EventHub",
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    message = "Health check failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                service = "EventHub MP3 Sender",
                version = "1.0.0",
                description = "Sends MP3 files to Azure EventHub in chunks",
                endpoints = new
                {
                    upload = "/api/mp3files/upload",
                    health = "/api/mp3files/health",
                    info = "/api/mp3files/info"
                },
                limits = new
                {
                    maxFileSize = "100MB",
                    supportedFormats = new[] { "MP3" },
                    chunkSize = "1MB"
                }
            });
        }
    }
}