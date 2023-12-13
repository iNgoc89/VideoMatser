using MetaData.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    internal interface IDeleteImageCameraService
    {
        Task RunDeleteImage(CancellationToken stoppingToken);
    }
    internal class DeleteImageProcessingService : IDeleteImageCameraService
    {
        public IConfiguration _configuration;
        public WorkDeleteService _workDelete;
        public long ThuMucLay = 0;
        public double TimeDelete = 0;
        public DeleteImageProcessingService(IConfiguration configuration, WorkDeleteService workDelete)
        {
            _configuration = configuration;
            _workDelete = workDelete;
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:ImageDelete"] ?? "0");
            TimeDelete = double.Parse(_configuration["TimeDelete:Time"] ?? "0");
        }
        public async Task RunDeleteImage(CancellationToken stoppingToken)
        {
            if (ThuMucLay > 0 && TimeDelete > 0)
            {
                await _workDelete.DeleteFiles(ThuMucLay, TimeDelete, stoppingToken);
            }
        }

    }
}
