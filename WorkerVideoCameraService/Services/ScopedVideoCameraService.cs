using MetaData.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    public class ScopedVideoCameraService : BackgroundService
    {
        public IServiceProvider _services { get; }
        private readonly ILogger<ScopedVideoCameraService> _logger;
        CameraData CameraData;
        public ScopedVideoCameraService(IServiceProvider services, ILogger<ScopedVideoCameraService> logger)
        {
            _services = services;
            _logger = logger;
            CameraData = CameraData.getInstance();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service running.");
            stoppingToken.Register(() => _logger.LogInformation("Timed Hosted Service is stopping."));
            await RunApp(stoppingToken);
        }

        private async Task RunApp(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service is working.");

            using (var scope = _services.CreateScope())
            {
                try
                {
                    var scopedProcessingService =
                    scope.ServiceProvider
                     .GetRequiredService<IScopedVideoCameraService>();

                    await scopedProcessingService.RunApp(stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "{Message}", ex.Message);
                }
            
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service is stopping.");

            // Hủy tất cả tiến trình FFmpeg còn đang chạy
            foreach (var process in CameraData.ffmpegProcesses)
            {
                if (!process.HasExited)
                {
                    _logger.LogInformation($"Killing FFmpeg process for {process.StartInfo.Arguments}");
                    process.Kill();
                }
            }

            await base.StopAsync(stoppingToken);
        }
    }
}
