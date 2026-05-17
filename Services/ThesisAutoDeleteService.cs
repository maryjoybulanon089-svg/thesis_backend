using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThesisRepository.Data;

namespace ThesisRepository.Services
{
    public class ThesisAutoDeleteService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ThesisAutoDeleteService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

        public ThesisAutoDeleteService(
            IServiceProvider serviceProvider, 
            ILogger<ThesisAutoDeleteService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Thesis Auto-Delete Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWork(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Thesis Auto-Delete Service.");
                }

                // Wait 24 hours before checking again
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (context == null)
                {
                    _logger.LogWarning("ApplicationDbContext not available in scope.");
                    return;
                }

                // Calculate the threshold date (30 days ago)
                var thresholdDate = DateTime.UtcNow.AddDays(-30);

                // Use async query to avoid blocking threads; guard against connectivity issues
                var thesesToDelete = await context.Theses
                    .Where(t => t.Status == "rejected"
                             && !t.IsDeleted
                             && t.RejectedAt != null
                             && t.RejectedAt <= thresholdDate)
                    .ToListAsync(stoppingToken);

                if (thesesToDelete != null && thesesToDelete.Count > 0)
                {
                    _logger.LogInformation($"Found {thesesToDelete.Count} rejected theses older than 30 days. Soft-deleting them.");

                    foreach (var thesis in thesesToDelete)
                    {
                        thesis.IsDeleted = true;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Successfully soft-deleted old rejected theses.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while attempting to auto-delete theses. Will retry on next interval.");
            }
        }
    }
}
