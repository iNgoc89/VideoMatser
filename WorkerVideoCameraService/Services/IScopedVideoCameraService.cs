using Dapper;
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
        public readonly string? _connectionString;
        IDbConnection Connection { get { return new SqlConnection(_connectionString); } }

        public int idNew = 0;
        public int typeCamera = 0;
        public long? ThuMucId = null;
        public static string? TenThuMuc = string.Empty;
        public static string? ffmpeg = string.Empty;
        public static string? TenFile = string.Empty;
        public static string? DuongDanFile = string.Empty;
        public readonly DateTime timeRun = DateTime.Now;
        public ScopedProcessingService(IOTContext iOTContext, XmhtService xmhtService, IConfiguration configuration)
        {
            _iOTContext = iOTContext;
            _xmhtService = xmhtService;
            _configuration = configuration;
            _connectionString = configuration["ConnectionStrings:IOTConnection"];
            ffmpeg = _configuration["FFmpeg:Url"];
            typeCamera = int.Parse(_configuration["TypeCamera:Type"] ?? "2");
            TenThuMuc = _configuration["ThuMucNghiepVu:TenThuMuc"];
        }

        public async Task RunApp(CancellationToken stoppingToken)
        {
            long idThuMuc = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, TenThuMuc);
            var cameras = _iOTContext.Cameras.Where(x => x.Type == typeCamera).ToList();
            if (idThuMuc > 0)
            {
                long? ThuMucWSID = 0;
                string? ThuMucDuongDan = string.Empty;
                var kq = _xmhtService.TaoThuMuc(null, idThuMuc, DateTime.Now.ToString("yyyyMM"), ref ThuMucWSID, ref ThuMucDuongDan);
                if (kq > 0)
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        foreach (var cam in cameras)
                        {
                            var fileName = DateTime.Now.ToString("dd_hhmmss") + ".mp4";
                            TenFile = cam.Id + "_" + fileName;
                            DuongDanFile = Path.Combine(ThuMucDuongDan, TenFile);

                            //Lưu video
                            GetVideo(cam.RtspUrl, DuongDanFile);

                            P_VideoCamera_Insert(ref idNew, cam.Id, DateTime.Now, DateTime.Now.AddSeconds(5), DuongDanFile, 20);
                        }
                        await Task.Delay(5000, stoppingToken);

                        TimeSpan totalTimeRun = DateTime.Now.Subtract(timeRun);
                        CheckTotalTimeRun(totalTimeRun);
                    }
                 
                }

            }

         
        }
        public static void GetVideo(string rtspUrl, string contentRoot)
        {
            string cmdLine = $@"-t 5 -rtsp_transport tcp -i {rtspUrl} -vf scale=640:360 -r 24 -crf 23 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel verbose -an -hide_banner";

            Process process = new();
            process.StartInfo.FileName = ffmpeg;
            process.StartInfo.Arguments = cmdLine;
            process.Start();
        }

        //Kiểm tra điều kiện chạy lại, ví dụ thời gian chạy đã lâu;
        public static void CheckTotalTimeRun(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMinutes == 30)
            {
                Refresh();
            }
        }
        public static void Refresh()
        {
            Process process = new();
            process.StartInfo.FileName = ffmpeg;
            process.Refresh();
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
        public static void TestCMD(string contentRoot)
        {
            string cmdLine = $@"-t 5 -rtsp_transport tcp -i rtsp://admin:Hd123456@10.68.81.193:554/ -vf scale=704:576 -r 24 -crf 23 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel verbose -an -hide_banner";
            //string cmdLine = @"/C ""C:\Data\Files\text.txt""";
            Process process = new();
            var processStartInfo = new ProcessStartInfo(@"C:\Data\FFmpeg\bin\ffmpeg.exe")
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                Arguments = cmdLine
            };
            process.StartInfo = processStartInfo;
            process.Start();
        }

    }
}
