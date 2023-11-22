using FFmpegWebAPI.Data;
using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.Design;
using System.Diagnostics;
using static FFmpegWebAPI.Controllers.VideoController;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FFmpegWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        public readonly IOTContext _iOTContext;
        public IOTService _iOTService;
        public WorkVideoService _workVideoService;
        public XmhtService _xmhtService;
        public IConfiguration _configuration;
        public long? ThuMucLay = null;
        public long? ThuMucLuu = null;
        public long? ThuMucId = null;
        public string? ThuMucVirtual = string.Empty;

        public VideoController(IOTContext iOTContext, IOTService iOTService, WorkVideoService workVideoService, XmhtService xmhtService, IConfiguration configuration)
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _workVideoService = workVideoService;
            _xmhtService = xmhtService;
            _configuration = configuration;

            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:VideoCamera"] ?? "10043");
            ThuMucLuu = long.Parse(_configuration["ThuMucNghiepVu:ConcatVideoCamera"] ?? "10046");
            ThuMucVirtual = _configuration["ThuMucNghiepVu:ThuMucVirtual"];

        }

        // GET api/<VideoController>/GID

        [HttpGet("{GID}")]
        public IActionResult Get(Guid GID)
        {
            if (GID != Guid.Empty)
            {
                var data = _iOTContext.ConcatVideoCameras.Where(x => x.GID == GID).ToList();
                if (data.Count > 0)
                {
                    return new JsonResult(data);
                }
                else
                {
                    string message = "GID không tồn tại!";
                    return new JsonResult(message);
                }
            }

            return NoContent();
        }

        [HttpPost("ConcatVideo")]
        public IActionResult PostConcat([FromBody] VideoConcatRequest videoConcatRequest)
        {
            VideoReturl videoReturl = new();
            int kq = 0;


            if (videoConcatRequest.BeginDate > DateTime.MinValue && videoConcatRequest.EndDate > DateTime.MinValue)
            {
                //Kiểm tra GID đã tồn tại hay chưa
                List<ConcatVideoCamera> gid = _iOTContext.ConcatVideoCameras.Where(x => x.GID == videoConcatRequest.GID).ToList();
                if (gid.Count > 0)
                {
                    var kqGID = gid.First();

                    videoReturl.Id = kqGID.Id;
                    videoReturl.UrlPath = kqGID.VideoUri;
                    videoReturl.ErrMsg = "Đã tồn tại GID!";

                    return new JsonResult(videoReturl);


                }
                //Kiểm tra video tương tự trên hệ thống hay chưa
                List<ConcatVideoCamera> data = _iOTContext.ConcatVideoCameras.Where(x => x.BeginDate <= videoConcatRequest.BeginDate.AddSeconds(5) && x.BeginDate >= videoConcatRequest.BeginDate.AddSeconds(-5) && x.EndDate <= videoConcatRequest.EndDate.AddSeconds(5) && x.EndDate >= videoConcatRequest.EndDate.AddSeconds(-5)).ToList();
                if (data.Count > 0)
                {
                    var video = data.First();
                    videoReturl.Id = video.Id;
                    videoReturl.UrlPath = video.VideoUri;
                    videoReturl.ErrMsg = "Đã tồn tại video trên hệ thống!";

                    return new JsonResult(videoReturl);
                }

                //Kiểm tra có file phù hợp để ghép hay không
                try
                {
                    long? idThuMucLay = ThuMucLay;
                    long? idThuMucLuu = ThuMucLuu;
                    if (idThuMucLay > 0 && idThuMucLuu > 0)
                    {
                        var urlLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLay).Result;
                        var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLuu).Result;

                        long thuMucConId = 0;
                        var idThuMucLuuLay = _xmhtService.P_ThuMuc_LayTheoThuMucCha(null, idThuMucLay, videoConcatRequest.CameraId.ToString(), ref thuMucConId);
                        var urlLuuLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLuuLay).Result;
                        if (urlLay != null && urlLuu != null && idThuMucLuuLay > 0 && urlLuuLay != null)
                        {
                            var checkFile = _workVideoService.CheckFile(videoConcatRequest.CameraId, urlLuuLay.DuongDan, videoConcatRequest.BeginDate, videoConcatRequest.EndDate);
                            if (checkFile?.Length > 0)
                            {
                                kq = _iOTService.P_ConcatVideoCamera_Insert(videoConcatRequest.GID, videoConcatRequest.CameraId, videoConcatRequest.BeginDate, videoConcatRequest.EndDate);
                                if (kq > 0)
                                {
                                    var dateNow = DateTime.Now.ToString("yyyyMM");
                                    videoReturl.Id = kq;
                                    videoReturl.GID = videoConcatRequest.GID;
                                    videoReturl.UrlPath = $"/{ThuMucVirtual}/{DateTime.Now.ToString("yyyyMM")}/{videoConcatRequest.GID}.mp4";
                                    videoReturl.ErrMsg = "Tạo lệnh ghép thành công!";

                                    return new JsonResult(videoReturl);
                                }


                            }
                            else
                            {
                                videoReturl.ErrMsg = "Không có video phù hợp. Xem lại thời điểm lấy video!";
                                return new JsonResult(videoReturl);
                            }
                        }



                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }




            }

            return NoContent();
        }

        public class VideoConcatRequest
        {
            public Guid GID { get; set; }
            public int CameraId { get; set; }
            public DateTime BeginDate { get; set; }
            public DateTime EndDate { get; set; }
        }
    }
}
