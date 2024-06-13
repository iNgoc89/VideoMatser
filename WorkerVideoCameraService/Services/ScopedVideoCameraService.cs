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
        public ScopedVideoCameraService(IServiceProvider services, ILogger<ScopedVideoCameraService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunApp(stoppingToken);
        }

        private async Task RunApp(CancellationToken stoppingToken)
        {
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
            await base.StopAsync(stoppingToken);
        }
    }
}
