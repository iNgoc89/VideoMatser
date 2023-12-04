using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaData.Context;
using MetaData.Models;
using MetaData.Services;
using Microsoft.AspNetCore.Mvc;

namespace FFmpegWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        public IOTContext _iOTContext;
        public IOTService _iOTService;
        public WorkVideoService _workVideoService;
        public XmhtService _xmhtService;
        public IConfiguration _configuration;
        public long? ThuMucLay = null;
        public long? ThuMucLuu = null;
        public long? ThuMucId = null;
        public long? ThuMucTxt = null;
        public long? ThuMucImage = null;
        public string? ThuMucVirtual = string.Empty;
        public string? DuongDanFileLuu = string.Empty;
        public string? DuongDanFileTXT = string.Empty;
        public string? DuongDanFileImage = string.Empty;
        public string? TimeOut = string.Empty;
        public string? ffmpeg = string.Empty;
        public VideoController(IOTContext iOTContext, IOTService iOTService, WorkVideoService workVideoService, XmhtService xmhtService, IConfiguration configuration)
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _workVideoService = workVideoService;
            _xmhtService = xmhtService;
            _configuration = configuration;

            ThuMucLay = long.Parse(_configuration["ThuMucNghiepVu:VideoCamera"] ?? "10043");
            ThuMucLuu = long.Parse(_configuration["ThuMucNghiepVu:ConcatVideoCamera"] ?? "10046");
            ThuMucTxt = long.Parse(_configuration["ThuMucNghiepVu:CmdConcat"] ?? "10047");
            ThuMucImage = long.Parse(_configuration["ThuMucNghiepVu:ImageCamera"] ?? "10065");
            ThuMucVirtual = _configuration["ThuMucNghiepVu:ThuMucVirtual"];
            TimeOut = _configuration["TimeOutFFmpeg:Millisecond"] ?? "30000";

            ffmpeg = _configuration["FFmpeg:Url"];
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
        public async Task<JsonResult> PostConcat([FromBody] VideoConcatRequest videoConcatRequest)
        {
            VideoReturl videoReturl = new();
            int kq = 0;
            //Kiểm tra kiểu ngày tháng
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

                //Kiểm tra cameraId có tồn tại hay ko
                var checkCam = _iOTContext.Cameras.Where(x => x.Id == videoConcatRequest.CameraId && x.IsActive == true).ToList();
                if (checkCam.Count == 0)
                {
                    videoReturl.ErrMsg = "Camera không tồn tại trên hệ thống!";
                    return new JsonResult(videoReturl);
                }

                //Kiểm tra có file phù hợp để ghép hay không
                try
                {
                    if (ThuMucLay > 0 && ThuMucLuu > 0 && ThuMucTxt > 0 && !string.IsNullOrEmpty(TimeOut))
                    {
                        long? ThuMucWSID = 0;
                        string ThuMucDuongDan = string.Empty;
                        var thuMuc = _xmhtService.TaoThuMuc(null, ThuMucLuu, DateTime.Now.ToString("yyyyMM"), ref ThuMucWSID, ref ThuMucDuongDan);
                        var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc).Result;
                        var urlLay = _xmhtService.P_ThuMuc_LayTheoID(null, ThuMucLay).Result;
                        var urlTxt = _xmhtService.P_ThuMuc_LayTheoID(null, ThuMucTxt).Result;

                        long thuMucConId = 0;
                        var idThuMucLuuLay = _xmhtService.P_ThuMuc_LayTheoThuMucCha(null, ThuMucLay, videoConcatRequest.CameraId.ToString(), ref thuMucConId);
                        var urlLuuLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLuuLay).Result;
                        if (urlLay != null && urlLuu != null && urlTxt != null && idThuMucLuuLay > 0 && urlLuuLay != null)
                        {
                            var checkFile = _workVideoService.CheckFile(videoConcatRequest.CameraId, urlLuuLay.DuongDan, videoConcatRequest.BeginDate, videoConcatRequest.EndDate);
                            if (checkFile?.Length > 0)
                            {
                                kq = _iOTService.P_ConcatVideoCamera_Insert(videoConcatRequest.GID, videoConcatRequest.CameraId, videoConcatRequest.BeginDate, videoConcatRequest.EndDate);
                                if (kq > 0)
                                {
                                    try
                                    {
                                        var dateNow = DateTime.Now.ToString("yyyyMM");
                                        var fileName = videoConcatRequest.GID + ".mp4";
                                        var fileNameTxt = videoConcatRequest.GID + ".txt";

                                        DuongDanFileLuu = Path.Combine(urlLuu.DuongDan, fileName);
                                        DuongDanFileTXT = Path.Combine(urlTxt.DuongDan, fileNameTxt);

                                        //Concat Video
                                        await _workVideoService.ConcatVideo(videoConcatRequest.CameraId, urlLuuLay.DuongDan, DuongDanFileTXT, videoConcatRequest.BeginDate, videoConcatRequest.EndDate, ffmpeg, DuongDanFileLuu, TimeOut);

                                        //Check file tồn tại
                                        if (System.IO.File.Exists(DuongDanFileLuu))
                                        {
                                            //Update table ConcatVideoCamera
                                            if (!string.IsNullOrEmpty(ThuMucVirtual))
                                            {
                                                var videoUri = $"/{ThuMucVirtual}/" + dateNow + "/" + fileName;
                                                _iOTService.P_ConcatVideoCamera_Update(kq, videoUri, 20);

                                                videoReturl.Id = kq;
                                                videoReturl.GID = videoConcatRequest.GID;
                                                videoReturl.UrlPath = videoUri;
                                                videoReturl.ErrMsg = "Tạo lệnh ghép thành công!";

                                                return new JsonResult(videoReturl);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        videoReturl.ErrMsg = ex.Message;
                                        return new JsonResult(videoReturl);
                                    }
                                }

                                videoReturl.ErrMsg = "Insert được db nhưng lỗi concat";
                                return new JsonResult(videoReturl);

                            }
                            else
                            {
                                videoReturl.ErrMsg = "Không có video phù hợp. Xem lại thời điểm lấy video!";
                                return new JsonResult(videoReturl);
                            }
                        }

                        videoReturl.ErrMsg = "Không lấy được thư mục";
                        return new JsonResult(videoReturl);

                    }
                }
                catch (Exception ex)
                {

                    videoReturl.ErrMsg = ex.Message;
                    return new JsonResult(videoReturl);
                }

            }
            videoReturl.ErrMsg = "Kiểu dữ liệu ngày tháng không hợp lệ!";
            return new JsonResult(videoReturl);
        }

        [HttpPost("Image")]
        public async Task<JsonResult> PostImage([FromBody] ImageRequest imageRequest)
        {
            ImageReturl imageReturl = new();
            //Kiểm tra cameraId
            var cameras = _iOTContext.Cameras.Where(x => x.Id == imageRequest.CameraId && x.IsActive == true).ToList();
            if (cameras.Count > 0 && ThuMucImage > 0)
            {
                var camera = cameras.First();
                var urlImage = _xmhtService.P_ThuMuc_LayTheoID(null, ThuMucImage).Result;
                if (urlImage != null && !string.IsNullOrEmpty(TimeOut))
                {
                    var fileName = imageRequest.CameraId + "_" + DateTime.Now.Ticks.ToString() + ".jpg";
                    DuongDanFileImage = Path.Combine(urlImage.DuongDan, fileName);

                    await _workVideoService.GetImage(ffmpeg, camera.RtspUrl, DuongDanFileImage, TimeOut);

                    //Kiểm tra file đã ghi hay chưa
                    if (System.IO.File.Exists(DuongDanFileImage))
                    {
                        var base64Image = _workVideoService.ImageToBase64(DuongDanFileImage);
                        if (!string.IsNullOrEmpty(base64Image))
                        {
                            imageReturl.Base64 = base64Image;
                            imageReturl.ErrMsg = "Tạo base64 cho image thành công!";
                            return new JsonResult(imageReturl);
                        }

                        imageReturl.ErrMsg = "Lỗi tạo bas64Image!";
                        return new JsonResult(imageReturl);
                    }

                    imageReturl.ErrMsg = "Lỗi file không tồn tại!";
                    return new JsonResult(imageReturl);
                }

                imageReturl.ErrMsg = "Đường dẫn lưu image không tồn tại!";
                return new JsonResult(imageReturl);
            }

            imageReturl.ErrMsg = "Camera không hợp lệ!";
            return new JsonResult(imageReturl);
        }
    }
}
