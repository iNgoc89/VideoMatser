using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    public class DeleteTxtService : BackgroundService
    {
        public IServiceProvider _services { get; }
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        public DeleteTxtService(IServiceProvider services, IHostApplicationLifetime hostApplicationLifetime)
        {
            _services = services;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunDeleteFile(stoppingToken);
        }

        private async Task RunDeleteFile(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var scopedProcessingService =
                    scope.ServiceProvider
                    .GetRequiredService<IDeleteTxtService>();

                    await scopedProcessingService.RunDeleteFile(stoppingToken);
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
