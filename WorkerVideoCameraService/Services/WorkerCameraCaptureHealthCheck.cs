using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WorkerVideoCameraService.Services
{
    public class WorkerCameraCaptureHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly WorkerHealthState _healthState;

        public WorkerCameraCaptureHealthCheck(IConfiguration configuration, WorkerHealthState healthState)
        {
            _configuration = configuration;
            _healthState = healthState;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_healthState.LastException != null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "The camera capture worker reported a recent failure.",
                    _healthState.LastException));
            }

            if (!_healthState.LastCycleStartedAt.HasValue)
            {
                return Task.FromResult(HealthCheckResult.Degraded("The camera capture worker has not started a cycle yet."));
            }

            var timeVideo = int.Parse(_configuration["TimeVideo"] ?? "20000");
            var maxIdleTime = TimeSpan.FromMilliseconds(Math.Max(timeVideo * 3, 30000));
            var lastSignal = _healthState.LastCaptureCompletedAt ?? _healthState.LastCycleStartedAt.Value;

            if (DateTimeOffset.UtcNow - lastSignal > maxIdleTime)
            {
                return Task.FromResult(HealthCheckResult.Degraded("The camera capture worker heartbeat is stale."));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Camera capture worker is running. Active tasks: {_healthState.ActiveCaptureTasks}."));
        }
    }
}
