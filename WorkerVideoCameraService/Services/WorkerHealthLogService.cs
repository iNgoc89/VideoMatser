using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerVideoCameraService.Services
{
    public class WorkerHealthLogService : BackgroundService
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<WorkerHealthLogService> _logger;

        public WorkerHealthLogService(
            HealthCheckService healthCheckService,
            ILogger<WorkerHealthLogService> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var report = await _healthCheckService.CheckHealthAsync(stoppingToken);
                    if (report.Status != HealthStatus.Healthy)
                    {
                        _logger.LogWarning("Worker health status: {Status}", report.Status);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run worker health checks.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
