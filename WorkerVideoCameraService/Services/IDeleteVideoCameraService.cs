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
        public double TimeDelete = 0;
        public DeleteProcessingService(IHostEnvironment environment, XmhtService xmhtService, IConfiguration configuration, WorkVideoService workVideo)
        {
            _configuration = configuration;
            _environment = environment;
            _xmhtService = xmhtService;
            TenThuMuc = _configuration["ThuMucNghiepVu:TenThuMuc"];
            TimeDelete = double.Parse(_configuration["TimeDelete:Time"] ?? "5");
            _workVideo = workVideo;
        }
        public async Task RunDeleteFile(CancellationToken stoppingToken)
        {
            long idThuMuc = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, TenThuMuc);
            if (idThuMuc > 0)
            {
                var kq = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMuc).Result;
                if (kq != null)
                {
                    string? ThuMucDuongDan = kq.DuongDan;

                    string[] files = Directory.GetFiles(ThuMucDuongDan);
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        foreach (string file in files)
                        {
                            if (file.Length > 0)
                            {
                                var cutright = file[..^4];

                                var cutleft = cutright[5..];

                                DateTime? dateTime;
                                if (cutleft.Length > 0)
                                {
                                    long datetimeFile = long.Parse(cutleft);

                                    dateTime = new DateTime(datetimeFile);

                                    if (dateTime < DateTime.Now.AddMinutes(- TimeDelete))
                                    {
                                        _workVideo.DeleteFile(ThuMucDuongDan);
                                    }
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
