using FFmpegWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    internal interface IConcatVideoCameraService
    {
        Task RunConcatFile(CancellationToken stoppingToken);
    }
    internal class ConcatProcessingService : IConcatVideoCameraService
    {
        public IConfiguration _configuration;
        public IHostEnvironment _environment;
        public XmhtService _xmhtService;
        public IOTContext _iOTContext;
        public long? ThuMucId = null;
        public static string? TenThuMuc = string.Empty;
        public static string? DuongDanFile = string.Empty;
        public ConcatProcessingService(IHostEnvironment environment, XmhtService xmhtService, IConfiguration configuration, IOTContext iOTContext)
        {
            _configuration = configuration;
            _environment = environment;
            _xmhtService = xmhtService;
            _iOTContext = iOTContext;
            TenThuMuc = _configuration["ThuMucNghiepVu:ConcatVideoCamera"];
        }
        public async Task RunConcatFile(CancellationToken stoppingToken)
        {
            long idThuMuc = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, TenThuMuc);
            if (idThuMuc > 0)
            {
                long? ThuMucWSID = 0;
                string? ThuMucDuongDan = string.Empty;
                var kq = _xmhtService.TaoThuMuc(null, idThuMuc, DateTime.Now.ToString("yyyyMM"), ref ThuMucWSID, ref ThuMucDuongDan);
                if (kq > 0)
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var requets = _iOTContext.ConcatVideoCameras.Where(x => x.VideoUri == string.Empty && x.Status == 20).ToList();
                        if (requets.Count > 0)
                        {
                            foreach (var item in requets)
                            {

                            }
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                }
            }

        }

        public void DeleteFile(string filePath)
        {
            System.IO.File.Delete(filePath);
        }
    }
}
