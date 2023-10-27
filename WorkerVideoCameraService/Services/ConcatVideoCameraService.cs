using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    public class ConcatVideoCameraService : BackgroundService
    {
        public IServiceProvider _services { get; }
        public ConcatVideoCameraService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunConcatFile(stoppingToken);
        }

        private async Task RunConcatFile(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IConcatVideoCameraService>();

                await scopedProcessingService.RunConcatFile(stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
    }
}
