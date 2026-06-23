using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MoodBite.Data;

namespace MoodBite.Services
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(ApplicationDbContext db, ILogger<DatabaseHealthCheck> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Database is not reachable.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException or TimeoutException)
            {
                _logger.LogWarning(ex, "Database health check failed.");
                return HealthCheckResult.Unhealthy("Database is not reachable.");
            }
        }
    }
}
