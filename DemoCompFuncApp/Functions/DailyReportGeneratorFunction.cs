using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Microsoft.Azure.Functions.Worker.Extensions.Sql; // Add this line
using System.Text;

namespace DemoCompFuncApp.Functions;

/// <summary>
/// Timer Trigger Function - Daily Sales Report Generator
/// Real-world scenario: Automated daily report generation
/// Runs every day at 9:00 AM UTC to generate sales reports
/// CRON format: "0 0 9 * * *" = At 09:00:00 AM every day
/// Stores report to SQL database
/// </summary>
public class DailyReportGeneratorFunction
{
    private readonly ILogger _logger;

    public DailyReportGeneratorFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DailyReportGeneratorFunction>();
    }

    [Function("DailyReportGenerator")]
    [SqlOutput("dbo.DailyReports", "SqlConnectionString")]
    public async Task<DailyReport> Run(
        [TimerTrigger("0 30 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Daily Report Generator executed at: {DateTime.UtcNow}");
        _logger.LogInformation($"Next execution scheduled at: {myTimer.ScheduleStatus?.Next}");

        // Simulate generating a sales report
        var reportDate = DateTime.UtcNow.Date.AddDays(-1); // Previous day's report
        var report = await GenerateSalesReport(reportDate);

        _logger.LogInformation($"Daily sales report generated and saved to SQL for {reportDate:yyyy-MM-dd}");

        return report;
    }

    private async Task<DailyReport> GenerateSalesReport(DateTime reportDate)
    {
        // Simulate report generation (in real scenario, query from database)
        await Task.Delay(100); // Simulate processing

        // Simulate some metrics (in real scenario, these would come from database)
        var random = new Random(reportDate.Day);
        var totalOrders = random.Next(50, 200);
        var totalRevenue = random.Next(5000, 25000);
        var avgOrderValue = totalRevenue / totalOrders;
        var newCustomers = random.Next(10, 50);

        var reportContent = new StringBuilder();
        reportContent.AppendLine("═══════════════════════════════════════════════════════");
        reportContent.AppendLine($"       DAILY SALES REPORT - {reportDate:yyyy-MM-dd}");
        reportContent.AppendLine("═══════════════════════════════════════════════════════");
        reportContent.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        reportContent.AppendLine();
        reportContent.AppendLine("SUMMARY:");
        reportContent.AppendLine("─────────────────────────────────────────────────────");
        reportContent.AppendLine($"Total Orders:          {totalOrders}");
        reportContent.AppendLine($"Total Revenue:         ${totalRevenue:N2}");
        reportContent.AppendLine($"Average Order Value:   ${avgOrderValue:N2}");
        reportContent.AppendLine($"New Customers:         {newCustomers}");
        reportContent.AppendLine();
        reportContent.AppendLine("TOP PRODUCTS:");
        reportContent.AppendLine("─────────────────────────────────────────────────────");
        reportContent.AppendLine("1. Laptop Pro 15          - 45 units - $45,000");
        reportContent.AppendLine("2. Wireless Mouse         - 89 units - $2,670");
        reportContent.AppendLine("3. USB-C Hub              - 67 units - $2,010");
        reportContent.AppendLine("4. Mechanical Keyboard    - 34 units - $3,400");
        reportContent.AppendLine("5. Monitor 27\"            - 23 units - $5,750");
        reportContent.AppendLine();
        reportContent.AppendLine("REGIONAL BREAKDOWN:");
        reportContent.AppendLine("─────────────────────────────────────────────────────");
        reportContent.AppendLine($"North America:         ${totalRevenue * 0.45:N2} (45%)");
        reportContent.AppendLine($"Europe:                ${totalRevenue * 0.30:N2} (30%)");
        reportContent.AppendLine($"Asia Pacific:          ${totalRevenue * 0.20:N2} (20%)");
        reportContent.AppendLine($"Other:                 ${totalRevenue * 0.05:N2} (5%)");
        reportContent.AppendLine();
        reportContent.AppendLine("═══════════════════════════════════════════════════════");
        reportContent.AppendLine("End of Report");
        reportContent.AppendLine("═══════════════════════════════════════════════════════");

        // Create the report object for SQL output
        var report = new DailyReport
        {
            ReportContent = reportContent.ToString()
        };

        return report;
    }

    // Model class for SQL table
    public class DailyReport
    {
        public int ReportId { get; set; }
        public string ReportContent { get; set; } = string.Empty;
    }
}