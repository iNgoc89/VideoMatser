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
        public ScopedVideoCameraService(IServiceProvider services)
        {
            _services = services;
        }
       
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunApp(stoppingToken);
        }

        private async Task RunApp(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IScopedVideoCameraService>();

                await scopedProcessingService.RunApp(stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
    }
}
