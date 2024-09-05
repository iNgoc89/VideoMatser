using Dapper;
using MetaData.Context;
using MetaData.Data;
using MetaData.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
        public IOTService _iOTService;
        public XmhtService _xmhtService;
        public IConfiguration _configuration;
        public WorkVideoService _workVideo;

        public int idNew = 0;
        public int TypeVideo = 0;
        public long? ThuMucId = null;
        public static string? ffmpeg = string.Empty;
        public static string? DuongDanFile = string.Empty;
        public static string TimeOut = string.Empty;
        public readonly DateTime timeRun = DateTime.Now;
        public long? ThuMucLay = null;

        CameraData CameraData;

        public ScopedProcessingService(IOTContext iOTContext, IOTService iOTService, XmhtService xmhtService, IConfiguration configuration, WorkVideoService workVideo)
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _xmhtService = xmhtService;
            _workVideo = workVideo;
            _configuration = configuration;

            ffmpeg = _configuration["FFmpeg:Url"];
            TypeVideo = int.Parse(_configuration["TypeCamera:TypeVideo"] ?? "0");
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:VideoDelete"] ?? "0");
            TimeOut = _configuration["TimeOutFFmpeg:Millisecond"] ?? "0";

            CameraData = CameraData.getInstance();
            if (CameraData.Cameras.Count == 0)
            {
                CameraData.Cameras = _iOTService.GetCameras().Where(x=>x.BusinessId == TypeVideo).ToList();
            }
        }

        public async Task RunApp(CancellationToken stoppingToken)
        {
            if (ThuMucLay > 0 && TimeOut != "0" && TypeVideo > 0)
            {
                if (CameraData.Cameras.Count > 0)
                {
                    var tasks = new List<Task>();
                    foreach (var cam in CameraData.Cameras)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            while (!stoppingToken.IsCancellationRequested)
                            {
                            
                                long? ThuMucWSID = 0;
                                string ThuMucDuongDan = string.Empty;
                                var thuMuc = _xmhtService.TaoThuMuc(null, ThuMucLay, cam.CameraId.ToString(), ref ThuMucWSID, ref ThuMucDuongDan);

                                var fileName = cam.CameraId.ToString() + "_" + DateTime.Now.Ticks.ToString() + ".mp4";
                                var camId = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc);
                                if (camId != null && thuMuc > 0)
                                {
                                    DuongDanFile = Path.Combine(camId.DuongDan, fileName);

                                    //Lưu video
                                    await _workVideo.GetVideo(ffmpeg, cam.RtspUrl, DuongDanFile, TimeOut, stoppingToken);
                                }
                            }
                        }, stoppingToken));
                      
                    }
                    await Task.WhenAll(tasks);
                }
                //await Task.Delay(5000, stoppingToken);
            }


        }
    }
}
