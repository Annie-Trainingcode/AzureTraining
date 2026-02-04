using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Azure.Functions.Worker.Extensions.Sql; // Add this line


namespace DemoCompFuncApp.Functions;

/// <summary>
/// HTTP Trigger with SQL Input Binding - Daily Report Reader
/// Simple demonstration: Reads daily report from SQL database and returns as HTTP response
/// </summary>
public class DailyReportReaderFunction
{
    private readonly ILogger _logger;

    public DailyReportReaderFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DailyReportReaderFunction>();
    }

    [Function("DailyReportReader")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
        [SqlInput("SELECT TOP 1 ReportId, ReportContent FROM dbo.DailyReports ORDER BY ReportId DESC",
            "SqlConnectionString")] IEnumerable<DailyReport> reports)
    {
        _logger.LogInformation($"Reading daily report from SQL database");

        var response = req.CreateResponse(HttpStatusCode.OK);

        try
        {
            var reportList = reports.ToList();

            if (!reportList.Any())
            {
                response = req.CreateResponse(HttpStatusCode.NotFound);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteAsJsonAsync(new
                {
                    message = "No daily reports found in database",
                    hint = "Run the DailyReportGenerator function first to create a report.",
                    timestamp = DateTime.UtcNow
                });
                return response;
            }

            var latestReport = reportList.First();

            // Return as plain text response
            response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync($"Report ID: {latestReport.ReportId}\n\n");
            await response.WriteStringAsync(latestReport.ReportContent);

            _logger.LogInformation($"Successfully retrieved report ID: {latestReport.ReportId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading report from SQL: {ex.Message}");
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteAsJsonAsync(new
            {
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }

        return response;
    }

    // Model class matching SQL table
    public class DailyReport
    {
        public int ReportId { get; set; }
        public string ReportContent { get; set; } = string.Empty;
    }
}