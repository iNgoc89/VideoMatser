using Dapper;
using MetaData.Context;
using MetaData.Data;
using MetaData.Models;
using MetaData.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.PowerShell.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerVideoCameraService.Services
{
    internal interface IScopedVideoCameraService
    {
        Task RunApp(CancellationToken stoppingToken);
        Task StopApp(CancellationToken stoppingToken);
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
        public int TimeVideo = 0;
        public int TimeProcess = 0;
        public readonly DateTime timeRun = DateTime.Now;
        public long? ThuMucLay = null;
        public bool RunOne = false;

        CameraData CameraData;
        private readonly List<Task> _tasks = new List<Task>();
        private readonly object _tasksLock = new object();
        private readonly ILogger<ScopedProcessingService> _logger;
        private readonly WorkerHealthState _healthState;

        public ScopedProcessingService(
            IOTContext iOTContext,
            IOTService iOTService,
            XmhtService xmhtService,
            IConfiguration configuration,
            WorkVideoService workVideo,
            ILogger<ScopedProcessingService> logger,
            WorkerHealthState healthState)
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _xmhtService = xmhtService;
            _workVideo = workVideo;
            _configuration = configuration;
            _logger = logger;
            _healthState = healthState;

            ffmpeg = _configuration["FFmpeg:Url"];
            TypeVideo = int.Parse(_configuration["TypeCamera:TypeVideo"] ?? "0");
            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:VideoDelete"] ?? "0");
            TimeOut = _configuration["TimeOutFFmpeg:Millisecond"] ?? "0";
            TimeVideo = int.Parse(_configuration["TimeVideo"] ?? "20000");
            TimeProcess = int.Parse(_configuration["TimeProcess"] ?? "50");

            CameraData = CameraData.getInstance();
        }

        public async Task RunApp(CancellationToken stoppingToken)
        {
            if (ThuMucLay > 0 && TimeOut != "0" && TypeVideo > 0 && TimeVideo > 0 && TimeProcess > 0)
            {
                if (CameraData.Cameras.Count == 0)
                {
                    CameraData.Cameras = (await _iOTService.GetCamerasAsync(stoppingToken))
                        .Where(x => x.BusinessId == TypeVideo)
                        .ToList();
                }

                if (CameraData.Cameras.Count > 0)
                {
                    var timeVideo = TimeVideo / 1000;


                    while (!stoppingToken.IsCancellationRequested)
                    {
                        CleanupCompletedTasks();
                        var cameDangChay = await _iOTService.GetCamerasDangChayAsync(stoppingToken);
                        _healthState.MarkCycleStarted(GetActiveTaskCount());

                        var dateNow1 = DateTime.Now;
                        foreach (var cam in cameDangChay)
                        {
                            AddCaptureTask(Task.Run(
                                () => CaptureVideoAsync(cam, timeVideo, stoppingToken),
                                stoppingToken));

                            await Task.Delay(TimeProcess, stoppingToken);
                        }
                        var dateNow2 = DateTime.Now;
                        CleanupCompletedTasks();

                        //TimeSpan timeSpan = new();
                        //if (dateNow2 > dateNow1)
                        //{
                        //    timeSpan = dateNow2 - dateNow1;
                        //}
                        //else
                        //{
                        //    timeSpan = new TimeSpan(100);
                        //}

                        TimeSpan timeSpan = dateNow2 - dateNow1;

                        //if (RunOne == false)
                        //{
                        //    RunOne = true;
                        //    if (TimeVideo < (2 * timeSpan.TotalSeconds))
                        //    {
                        //        TimeVideo = 2 * (int)timeSpan.TotalSeconds;
                        //    }
                        //}
                      

                        int delay = TimeVideo - (int)timeSpan.TotalMilliseconds - 20;
                        if (delay < 0)
                        {
                            delay = 0;
                        }

                       // await Task.Delay(TimeVideo - (1000 * timeSpan.Seconds) - timeSpan.Milliseconds - 20, stoppingToken);
                       await Task.Delay(delay,stoppingToken);

                    }
                    //await Task.WhenAll(tasks);

                }

            }
        }

        private async Task CaptureVideoAsync(CameraModel cam, int timeVideo, CancellationToken stoppingToken)
        {
            try
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
                    await _workVideo.GetVideo(timeVideo.ToString(), ffmpeg, cam.RtspUrl, DuongDanFile, TimeOut, stoppingToken);
                    _healthState.MarkCaptureCompleted();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _healthState.MarkFailure(ex);
                _logger.LogError(ex, "Failed to capture video for camera {CameraId}.", cam.CameraId);
            }
        }

        private void AddCaptureTask(Task task)
        {
            lock (_tasksLock)
            {
                _tasks.Add(task);
                _healthState.SetActiveCaptureTasks(_tasks.Count);
            }
        }

        private void CleanupCompletedTasks()
        {
            lock (_tasksLock)
            {
                _tasks.RemoveAll(task => task.IsCompleted);
                _healthState.SetActiveCaptureTasks(_tasks.Count);
            }
        }

        private int GetActiveTaskCount()
        {
            lock (_tasksLock)
            {
                return _tasks.Count;
            }
        }

        public async Task StopApp(CancellationToken stoppingToken)
        {
            Task[] activeTasks;
            lock (_tasksLock)
            {
                activeTasks = _tasks.ToArray();
            }

            if (activeTasks.Length > 0)
            {
                try
                {
                    await Task.WhenAll(activeTasks);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "One or more capture tasks failed while stopping.");
                }
            }

            //Hủy tất cả tiến trình FFmpeg còn đang chạy
            foreach (var process in CameraData.ffmpegProcesses)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }

            // Dừng tất cả các process thuộc Job Object
            _workVideo.StopProcess();
        }
    }
}
