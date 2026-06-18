using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MetaData.Services
{
    public class CameraStatusHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly IOTService _iotService;

        public CameraStatusHealthCheck(IConfiguration configuration, IOTService iotService)
        {
            _configuration = configuration;
            _iotService = iotService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var typeVideo = int.Parse(_configuration["TypeCamera:TypeVideo"] ?? "0");
            var configuredCameras = (await _iotService.GetCamerasAsync(cancellationToken))
                .Where(camera => camera.BusinessId == typeVideo)
                .ToList();
            var runningCameras = await _iotService.GetCamerasDangChayAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["typeVideo"] = typeVideo,
                ["configuredCount"] = configuredCameras.Count,
                ["runningCount"] = runningCameras.Count,
                ["configuredCameras"] = configuredCameras.Select(ToHealthCamera).ToList(),
                ["runningCameras"] = runningCameras.Select(ToHealthCamera).ToList()
            };

            return HealthCheckResult.Healthy(
                $"Running cameras: {runningCameras.Count}/{configuredCameras.Count}.",
                data);
        }

        private static object ToHealthCamera(MetaData.Models.CameraModel camera)
        {
            return new
            {
                camera.BusinessId,
                camera.CameraId,
                camera.Code,
                camera.Description,
                camera.Name,
                camera.Type
            };
        }
    }
}
