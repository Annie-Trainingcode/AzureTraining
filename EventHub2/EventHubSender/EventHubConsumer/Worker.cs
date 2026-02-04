using EventHubConsumer.Services;

namespace EventHubConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMp3FileReassemblyService _reassemblyService;

    public Worker(ILogger<Worker> logger, IMp3FileReassemblyService reassemblyService)
    {
        _logger = logger;
        _reassemblyService = reassemblyService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MP3 File Status Monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var completedFiles = await _reassemblyService.GetCompletedFileIds();

                if (completedFiles.Any())
                {
                    _logger.LogInformation($"Currently have {completedFiles.Length} completed MP3 files");

                    foreach (var fileId in completedFiles.Take(5)) // Log first 5
                    {
                        var metadata = await _reassemblyService.GetFileMetadataAsync(fileId);
                        if (metadata != null)
                        {
                            _logger.LogInformation($"  - {metadata.FileName} ({metadata.FileSizeBytes} bytes)");
                        }
                    }

                    if (completedFiles.Length > 5)
                    {
                        _logger.LogInformation($"  ... and {completedFiles.Length - 5} more files");
                    }
                }

                await Task.Delay(30000, stoppingToken); // Check every 30 seconds
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in status monitor");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
