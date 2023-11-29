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
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        public ScopedVideoCameraService(IServiceProvider services, IHostApplicationLifetime hostApplicationLifetime)
        {
            _services = services;
            _hostApplicationLifetime = hostApplicationLifetime;
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
