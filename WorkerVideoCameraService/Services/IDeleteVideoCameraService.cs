using MetaData.Services;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    internal interface IDeleteVideoCameraService
    {
        Task RunDeleteVideo(CancellationToken stoppingToken);
    }

    internal class DeleteProcessingService : IDeleteVideoCameraService
    {
        public IConfiguration _configuration;
        public WorkDeleteService _workDelete;
        public long ThuMucLay = 0;
        public double TimeDelete = 0;
        public DeleteProcessingService(IConfiguration configuration, WorkDeleteService workDelete)
        {
            _configuration = configuration;
            _workDelete = workDelete;
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:VideoDelete"] ?? "0");
            TimeDelete = double.Parse(_configuration["TimeDelete:Time"] ?? "0");
        }
        public async Task RunDeleteVideo(CancellationToken stoppingToken)
        {
            if (ThuMucLay > 0 && TimeDelete > 0)
            {
                await _workDelete.DeleteFiles(ThuMucLay, TimeDelete, stoppingToken);
            }
        }

    }
}
