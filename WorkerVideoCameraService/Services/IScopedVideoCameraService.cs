﻿using Dapper;
using FFmpegWebAPI.Models;
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
        public readonly string? _connectionString;
        IDbConnection Connection { get { return new SqlConnection(_connectionString); } }

        public int idNew = 0;
        public int typeCamera = 0;
        public long? ThuMucId = null;
        public static string? TenThuMuc = string.Empty;
        public static string? ffmpeg = string.Empty;
        public static string? DuongDanFile = string.Empty;
        public readonly DateTime timeRun = DateTime.Now;
        public ScopedProcessingService(IOTContext iOTContext, XmhtService xmhtService, IConfiguration configuration, WorkVideoService workVideo)
        {
            _iOTContext = iOTContext;
            _xmhtService = xmhtService;
            _workVideo = workVideo;
            _configuration = configuration;
            _connectionString = configuration["ConnectionStrings:IOTConnection"];
            ffmpeg = _configuration["FFmpeg:Url"];
            typeCamera = int.Parse(_configuration["TypeCamera:Type"] ?? "2");
            TenThuMuc = _configuration["ThuMucNghiepVu:VideoCamera"];
        }

        public async Task RunApp(CancellationToken stoppingToken)
        {
            long idThuMuc = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, TenThuMuc);
            var cameras = _iOTContext.Cameras.Where(x => x.Type == typeCamera).ToList();
            if (idThuMuc > 0)
            {
                var kq = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMuc).Result;
                if (kq != null)
                {
                  
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        foreach (var cam in cameras)
                        {
                            var fileName = cam.Id + "_" + DateTime.Now.Ticks.ToString() + ".mp4";
                         
                            DuongDanFile = Path.Combine(kq.DuongDan, fileName);

                            //Lưu video
                            _workVideo.GetVideo(ffmpeg,cam.RtspUrl, DuongDanFile);

                            //P_VideoCamera_Insert(ref idNew, cam.Id, DateTime.Now, DateTime.Now.AddSeconds(5), DuongDanFile, 20);
                        }
                        await Task.Delay(5000, stoppingToken);

                        TimeSpan totalTimeRun = DateTime.Now.Subtract(timeRun);
                        _workVideo.Refresh(totalTimeRun, ffmpeg);
                    }
                 
                }

            }

         
        }
      
        //Insert video vào database
        public int P_VideoCamera_Insert(ref int id, int cameraId, DateTime beginDate, DateTime endDate, string videoUri, byte status)
        {
            using (var connection = Connection)
            {
                connection.Open();
                string sql = $"cmrs.P_VideoCamera_Insert";
                try
                {
                    var pars = new DynamicParameters();
                    pars.AddDynamicParams(new
                    {
                        Id = id,
                        CameraId = cameraId,
                        BeginDate = beginDate,
                        EndDate = endDate,
                        VideoUri = videoUri,
                        Status = status
                    });
                    pars.Add("Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ret = connection.Query<int>(sql: sql, param: pars,
                     commandType: CommandType.StoredProcedure);
                    int Id = pars.Get<int?>("Id") ?? 0;


                    return Id;
                }
                catch (Exception)
                {
                    //_logger.LogError(ex, $"Lỗi {System.Reflection.MethodInfo.GetCurrentMethod()}");
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) connection.Close();
                }
            }
            return 0;
        }
      
    }
}
