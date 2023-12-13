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
        public DeleteImageCameraService(IServiceProvider services)
        {
            _services = services;
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
