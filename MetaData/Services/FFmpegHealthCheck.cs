using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MetaData.Services
{
    public class FFmpegHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public FFmpegHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var configuredPath = _configuration["FFmpeg:Url"];
            var configuredPathExists = !string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath);
            var pathFfmpeg = FindExecutableInPath("ffmpeg.exe") ?? FindExecutableInPath("ffmpeg");

            if (!string.IsNullOrWhiteSpace(pathFfmpeg))
            {
                return Task.FromResult(HealthCheckResult.Healthy($"FFmpeg is available in PATH: {pathFfmpeg}."));
            }

            if (configuredPathExists)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"FFmpeg:Url exists, but current commands call 'ffmpeg' from PATH. Add {Path.GetDirectoryName(configuredPath)} to PATH or update command execution to use FFmpeg:Url."));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("FFmpeg is not available. Check FFmpeg:Url and system PATH."));
        }

        private static string? FindExecutableInPath(string executableName)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathValue))
            {
                return null;
            }

            foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(directory.Trim(), executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
