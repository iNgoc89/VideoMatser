using FFmpegWebAPI.Data;
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
        Task RunDeleteFile(CancellationToken stoppingToken);
    }

    internal class DeleteProcessingService : IDeleteVideoCameraService
    {
        public IConfiguration _configuration;
        public IHostEnvironment _environment; 
        public XmhtService _xmhtService;
        public WorkVideoService _workVideo;
        public long? ThuMucId = null;
        public static string? TenThuMuc = string.Empty;
        public static string? DuongDanFile = string.Empty;
        public DeleteProcessingService(IHostEnvironment environment, XmhtService xmhtService, IConfiguration configuration, WorkVideoService workVideo)
        {
            _configuration = configuration;
            _environment = environment;
            _xmhtService = xmhtService;
            TenThuMuc = _configuration["ThuMucNghiepVu:TenThuMuc"];
            _workVideo = workVideo;
        }
        public async Task RunDeleteFile(CancellationToken stoppingToken)
        {
            long idThuMuc = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, TenThuMuc);
            if (idThuMuc > 0)
            {
                string? ThuMucDuongDan = string.Empty;
                var kq = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMuc).Result;
                if (kq != null)
                {
                    ThuMucDuongDan = kq.DuongDan;
                    int dateNow = DateTime.Now.Day;
                    int timeNow = DateTime.Now.Hour;
                    string[] files = Directory.GetFiles(ThuMucDuongDan);
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        foreach (string file in files)
                        {
                            if (file.Length > 0)
                            {
                                var filename = file.Substring(file.Length - 19, 19);
                                var dateInFile = filename.Substring(0, 2);

                                int dateFile = 0;

                                if (dateInFile.Length == 2)
                                {
                                    dateFile = int.Parse(dateInFile);
                                }

                                int timeFile = 0;
                                var timeInFile = filename.Substring(3, 2);
                                if (timeInFile.Length == 2)
                                {
                                    timeFile = int.Parse(timeInFile);
                                }

                                if (dateFile > 0 && dateFile < dateNow || timeFile > 0 && timeFile < timeNow && timeNow - timeFile == 5)
                                {
                                   _workVideo.DeleteFile(file);
                                }
                           

                            }
                        }
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
         
        }

      
    }
}
