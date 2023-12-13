using MetaData.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    internal interface IDeleteTxtService
    {
        Task RunDeleteTxt(CancellationToken stoppingToken);
    }

    internal class DeleteTxtProcessingService : IDeleteTxtService
    {
        public IConfiguration _configuration;
        public WorkDeleteService _workDelete;
        public long ThuMucLay = 0;
        public double TimeDelete = 0;
        public DeleteTxtProcessingService(IConfiguration configuration, WorkDeleteService workDelete)
        {
            _configuration = configuration;
            _workDelete = workDelete;
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:CmdDelete"] ?? "0");
            TimeDelete = double.Parse(_configuration["TimeDelete:Time"] ?? "0");
        }
        public async Task RunDeleteTxt(CancellationToken stoppingToken)
        {
            if (ThuMucLay > 0 && TimeDelete > 0)
            {
                await _workDelete.DeleteFiles(ThuMucLay, TimeDelete, stoppingToken);
            }
        }

    }
}
