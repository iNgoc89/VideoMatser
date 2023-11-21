using Dapper;
using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    internal interface IScopedVideoCameraService
    {
        Task RunApp(CancellationToken stoppingToken);
    }
    internal class ScopedProcessingService : IScopedVideoCameraService
    {
        public IOTContext _iOTContext;
        public XmhtService _xmhtService;
        public IConfiguration _configuration;
        public WorkVideoService _workVideo;

        public int idNew = 0;
        public int typeCamera = 0;
        public long? ThuMucId = null;
        public static string? ffmpeg = string.Empty;
        public static string? DuongDanFile = string.Empty;
        public readonly DateTime timeRun = DateTime.Now;
        public long? ThuMucLay = null;
      
       
        public ScopedProcessingService(IOTContext iOTContext, XmhtService xmhtService, IConfiguration configuration, WorkVideoService workVideo)
        {
            _iOTContext = iOTContext;
            _xmhtService = xmhtService;
            _workVideo = workVideo;
            _configuration = configuration;

            ffmpeg = _configuration["FFmpeg:Url"];
            typeCamera = int.Parse(_configuration["TypeCamera:Type"] ?? "2");
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:VideoCamera"] ?? "10043");
            
        }

        public async Task RunApp(CancellationToken stoppingToken)
        {   
            
            long? idThuMuc = ThuMucLay;
            var cameras = _iOTContext.Cameras.Where(x => x.Type == typeCamera).ToList();
            if (idThuMuc > 0 && cameras.Count > 0)
            {
                var kq = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMuc).Result;
                if (kq != null)
                {
                  
                    while (!stoppingToken.IsCancellationRequested)
                    {
                     
                        foreach (var cam in cameras)
                        {
                            long? ThuMucWSID = 0;
                            string ThuMucDuongDan = string.Empty;
                            var thuMuc = _xmhtService.TaoThuMuc(null, idThuMuc, cam.Id.ToString(), ref ThuMucWSID, ref ThuMucDuongDan);

                            var fileName = cam.Id + "_" + DateTime.Now.Ticks.ToString() + ".mp4";
                            var camId = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc).Result;
                            if (camId != null && thuMuc > 0)
                            {
                                DuongDanFile = Path.Combine(camId.DuongDan, fileName);

                                //Lưu video
                                _workVideo.GetVideo(ffmpeg, cam.RtspUrl, DuongDanFile);
                            }

                       
                        }
                        await Task.Delay(5000, stoppingToken);

                        TimeSpan totalTimeRun = DateTime.Now.Subtract(timeRun);
                        _workVideo.Refresh(totalTimeRun, ffmpeg);
                    }
                 
                }

            }

         
        }
    }
}
