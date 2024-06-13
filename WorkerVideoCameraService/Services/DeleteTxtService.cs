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
        public DeleteTxtService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunDeleteTxt(stoppingToken);
        }

        private async Task RunDeleteTxt(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var scopedProcessingService =
                    scope.ServiceProvider
                    .GetRequiredService<IDeleteTxtService>();

                    await scopedProcessingService.RunDeleteTxt(stoppingToken);
                }
                catch
                {

                }
           

            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await base.StopAsync(stoppingToken);
        }
    }
}
