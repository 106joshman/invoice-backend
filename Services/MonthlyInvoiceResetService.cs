using Microsoft.EntityFrameworkCore;
using InvoiceService.Data;

namespace InvoiceService.Services;

public class MonthlyInvoiceResetService(
    IServiceScopeFactory scopeFactory,
    ILogger<MonthlyInvoiceResetService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<MonthlyInvoiceResetService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // ✅ Run ONLY on the 1st day of the month at 8:00 AM UTC
            if (now.Day == 1 && now.Hour == 7)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var businessesToReset = await context.Businesses
                        .Where(b =>
                            b.SubscriptionPlan == "Free" &&
                            (b.LastInvoiceReset == null ||
                             b.LastInvoiceReset.Value.Month != now.Month))
                        .ToListAsync(stoppingToken);

                    // ✅ Reset invoice count if new month
                    foreach (var user in businessesToReset)
                    {
                        user.MonthlyInvoiceCount = 0;
                        user.LastInvoiceReset = now;
                    }

                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Monthly invoice count reset completed at {Time}", now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while resetting monthly invoice counts");
                }

                // ⏳ Prevent running multiple times in the same hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            // ⏱️ Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
