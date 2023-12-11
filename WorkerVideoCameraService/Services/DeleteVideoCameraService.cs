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
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        public DeleteVideoCameraService(IServiceProvider services, IHostApplicationLifetime hostApplicationLifetime)
        {
            _services = services;
            _hostApplicationLifetime = hostApplicationLifetime;
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
                    _hostApplicationLifetime.StopApplication();
                }
         
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
    }
}
