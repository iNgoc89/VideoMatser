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
        public IHostEnvironment _environment;
        public XmhtService _xmhtService;
        public WorkVideoService _workVideo;
        public long? ThuMucId = null;
        public long? ThuMucLay = null;
        public static string? DuongDanFile = string.Empty;
        public double TimeDelete = 0;
        public DeleteTxtProcessingService(IHostEnvironment environment, XmhtService xmhtService, IConfiguration configuration, WorkVideoService workVideo)
        {
            _configuration = configuration;
            _environment = environment;
            _xmhtService = xmhtService;
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:CmdDelete"] ?? "0");
            TimeDelete = double.Parse(_configuration["TimeDelete:Time"] ?? "0");
            _workVideo = workVideo;
        }
        public async Task RunDeleteTxt(CancellationToken stoppingToken)
        {
            if (ThuMucLay > 0 && TimeDelete > 0)
            {
                var kq = _xmhtService.P_ThuMuc_LayTheoID(null, ThuMucLay).Result;
                if (kq != null)
                {
                    string? ThuMucDuongDan = kq.DuongDan;

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        string[] files = Directory.GetFiles(ThuMucDuongDan, "*", SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            if (file.Length > 0)
                            {
                                DateTime creation = File.GetCreationTime(file);

                                if (creation < DateTime.Now.AddMinutes(-TimeDelete))
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
