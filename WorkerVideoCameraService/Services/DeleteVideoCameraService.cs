using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    public class DeleteVideoCameraService : BackgroundService
    {
        public IServiceProvider _services { get; }
        public DeleteVideoCameraService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunDeleteVideo(stoppingToken);
        }

        private async Task RunDeleteVideo(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var scopedProcessingService =
                    scope.ServiceProvider
                    .GetRequiredService<IDeleteVideoCameraService>();

                    await scopedProcessingService.RunDeleteVideo(stoppingToken);
                }
                catch 
                {
                    
                }
                finally
                {
                    await StopAsync(stoppingToken);
                }
         
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
    }
}
