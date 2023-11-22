using Microsoft.Extensions.Hosting;
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
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        public ConcatVideoCameraService(IServiceProvider services, IHostApplicationLifetime hostApplicationLifetime)
        {
            _services = services;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunConcatFile(stoppingToken);
        }

        private async Task RunConcatFile(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var scopedProcessingService =
                   scope.ServiceProvider
                       .GetRequiredService<IConcatVideoCameraService>();

                    await scopedProcessingService.RunConcatFile(stoppingToken);
                }
                catch (Exception)
                {
                    throw;
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
