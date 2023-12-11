using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    public class DeleteImageCameraService : BackgroundService
    {
        public IServiceProvider _services { get; }
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        public DeleteImageCameraService(IServiceProvider services, IHostApplicationLifetime hostApplicationLifetime)
        {
            _services = services;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunDeleteImage(stoppingToken);
        }

        private async Task RunDeleteImage(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var scopedProcessingService =
                    scope.ServiceProvider
                    .GetRequiredService<IDeleteImageCameraService>();

                    await scopedProcessingService.RunDeleteImage(stoppingToken);
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
