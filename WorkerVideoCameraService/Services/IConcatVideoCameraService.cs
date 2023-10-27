using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
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
        public IOTService _iOTService;
        public WorkVideoService _workVideoService;
        public long? ThuMucId = null;
        public static string? ThuMucLay = string.Empty;
        public static string? DuongDanFileLuu = string.Empty;
        public static string? ThuMucLuu = string.Empty;
        public static string? ffmpeg = string.Empty;
  
        public ConcatProcessingService(IHostEnvironment environment, XmhtService xmhtService, IOTService iOTService,
            IConfiguration configuration, IOTContext iOTContext, WorkVideoService workVideoService)
        {
            _configuration = configuration;
            _environment = environment;
            _xmhtService = xmhtService;
            _iOTContext = iOTContext;
            _workVideoService = workVideoService;
            _iOTService = iOTService;
            ThuMucLay = _configuration["ThuMucNghiepVu:VideoCamera"];
            ThuMucLuu = _configuration["ThuMucNghiepVu:ConcatVideoCamera"];
            ffmpeg = _configuration["FFmpeg:Url"];

        }
        public async Task RunConcatFile(CancellationToken stoppingToken)
        {
            long idThuMucLay = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucLay);
            long idThuMucLuu = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucLuu);
            if (idThuMucLay > 0 && idThuMucLuu > 0)
            {
                var urlLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLay).Result;
                var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLuu).Result;
                if (urlLay != null && urlLuu != null)
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var requets = _iOTContext.ConcatVideoCameras.Where(x=> string.IsNullOrEmpty(x.VideoUri) && x.Status == 20).ToList();
                        if (requets.Count > 0)
                        {
                            foreach (var item in requets)
                            {
                                var fileName = item.Id + "_" + DateTime.Now.Ticks.ToString() + ".mp4";
                                DuongDanFileLuu = Path.Combine(urlLuu.DuongDan, fileName);

                                //Concat Video
                                _workVideoService.ConcatVideo(urlLay.DuongDan, item.BeginDate, item.EndDate, ffmpeg, DuongDanFileLuu);

                                //Update table ConcatVideoCamera
                                _iOTService.P_ConcatVideoCamera_Update(item.Id, DuongDanFileLuu);
                            }

                        }
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }

        }
    }
}
