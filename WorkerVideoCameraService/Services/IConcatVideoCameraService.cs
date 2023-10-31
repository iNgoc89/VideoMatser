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
        public static string? DuongDanFileTXT = string.Empty;
        public static string? ThuMucLuu = string.Empty;
        public static string? ThuMucTxt = string.Empty;
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
            ThuMucTxt = _configuration["ThuMucNghiepVu:CmdConcat"];
            ffmpeg = _configuration["FFmpeg:Url"];

        }
        public async Task RunConcatFile(CancellationToken stoppingToken)
        {
            long idThuMucLay = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucLay);
            long idThuMucLuu = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucLuu);
            long idThuMucTxt = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucTxt);
            if (idThuMucLay > 0 && idThuMucLuu > 0 && idThuMucTxt > 0)
            {
                var urlLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLay).Result;

                var urlTxt = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucTxt).Result;
                if (urlLay != null && urlTxt != null)
                {


                    while (!stoppingToken.IsCancellationRequested)
                    {
                        long? ThuMucWSID = 0;
                        string ThuMucDuongDan = string.Empty;
                        var thuMuc = _xmhtService.TaoThuMuc(null, idThuMucLuu, DateTime.Now.ToString("yyyyMM"), ref ThuMucWSID, ref ThuMucDuongDan);
                        var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc).Result;
                        var yc = _iOTContext.ConcatVideoCameras.Where(x => string.IsNullOrEmpty(x.VideoUri) && x.Status < 3).ToList();
                        if (yc.Count > 0 && thuMuc > 0 && urlLuu != null)
                        {
                            foreach (var item in yc)
                            {
                                var fileName = item.CameraId + "_" + item.BeginDate.Ticks + ".mp4";

                                var fileNameTxt = item.CameraId + "_" + item.BeginDate.Ticks + ".txt";

                                DuongDanFileLuu = Path.Combine(urlLuu.DuongDan, fileName);
                                DuongDanFileTXT = Path.Combine(urlTxt.DuongDan, fileNameTxt);


                                //Kiểm tra trước khi concat
                                var checkVideo = _iOTContext.ConcatVideoCameras.Where(x => x.CameraId == item.CameraId && x.BeginDate == item.BeginDate && x.EndDate == item.EndDate && item.Status == 20).ToList();
                                if (checkVideo.Count > 0)
                                {
                                    var urlFileLuu = checkVideo?.FirstOrDefault()?.VideoUri;
                                    if (!string.IsNullOrEmpty(urlFileLuu))
                                    {
                                        _iOTService.P_ConcatVideoCamera_Update(item.Id, urlFileLuu, 20);
                                    }
                                   
                                    continue;
                                }
                                var checkFile = _workVideoService.CheckFile(item.CameraId, urlLay.DuongDan, item.BeginDate, item.EndDate);
                                if (checkFile?.Length > 0)
                                {
                                    //Concat Video
                                    _workVideoService.ConcatVideo(item.CameraId, urlLay.DuongDan, DuongDanFileTXT, item.BeginDate, item.EndDate, ffmpeg, DuongDanFileLuu);

                                    //Check file tồn tại
                                    if (File.Exists(DuongDanFileLuu))
                                    {
                                        //Update table ConcatVideoCamera
                                        _iOTService.P_ConcatVideoCamera_Update(item.Id, DuongDanFileLuu, 20);
                                    }
                                }
                                else
                                {
                                    //Tăng status + 1 -> 3 thì dừng
                                    _iOTService.P_ConcatVideoCamera_UpdateStatus(item.Id, item.Status + 1);
                                }




                            }

                        }
                        await Task.Delay(3000, stoppingToken);
                    }
                }
            }

        }
    }
}
