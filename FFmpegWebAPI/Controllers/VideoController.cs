using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaData.Context;
using MetaData.Data;
using MetaData.Models;
using MetaData.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public WorkImageService _workImageService;
        public IConfiguration _configuration;
        public long? ThuMucVideoDelete = null;
        public long? ThuMucVideoSave = null;
        public long? ThuMucId = null;
        public long? ThuMucCmdDelete = null;
        public long? ThuMucImageSave = null;
        public long? ThuMucImageDelete = null;
        public string? VideoVirtual = string.Empty;
        public string? ImageVirtual = string.Empty;
        public string TimeOut = string.Empty;
        public static string? ffmpeg = string.Empty;
        public int TypeVideo = 0;
        public int TypeImage = 0;
        CameraData CameraData;
        public VideoController(IOTContext iOTContext, IOTService iOTService, WorkVideoService workVideoService, XmhtService xmhtService, WorkImageService workImageService, IConfiguration configuration)
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _workVideoService = workVideoService;
            _xmhtService = xmhtService;
            _workImageService = workImageService;
            _configuration = configuration;

            ThuMucVideoDelete = long.Parse(_configuration["ThuMucNghiepVu:VideoDelete"] ?? "0");
            ThuMucVideoSave = long.Parse(_configuration["ThuMucNghiepVu:VideoSave"] ?? "0");
            ThuMucCmdDelete = long.Parse(_configuration["ThuMucNghiepVu:CmdDelete"] ?? "0");
            TypeVideo = int.Parse(_configuration["TypeCamera:TypeVideo"] ?? "0");

            ThuMucImageSave = long.Parse(_configuration["ThuMucNghiepVu:ImageSave"] ?? "0");
            ThuMucImageDelete = long.Parse(_configuration["ThuMucNghiepVu:ImageDelete"] ?? "0");
            TypeImage = int.Parse(_configuration["TypeCamera:TypeImage"] ?? "0");

            VideoVirtual = _configuration["ThuMucNghiepVu:VideoVirtual"] ?? "";
            ImageVirtual = _configuration["ThuMucNghiepVu:ImageVirtual"] ?? "";
            TimeOut = _configuration["TimeOutFFmpeg:Millisecond"] ?? "0";

            ffmpeg = _configuration["FFmpeg:Url"];

            CameraData = CameraData.getInstance();
            if (CameraData.Cameras.Count == 0)
            {
                CameraData.Cameras = _iOTService.GetCameras().ToList();
            }

        }

        // GET api/<VideoController>/GID

        [HttpGet("{GID}")]
        public IActionResult Get(Guid GID)
        {
            if (GID != Guid.Empty)
            {
                var data = _iOTContext.ConcatVideoCameras.Where(x => x.Gid == GID).ToList();
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

            if (ThuMucVideoDelete == 0 || ThuMucVideoSave == 0 || ThuMucCmdDelete == 0)
            {
                videoReturl.ErrMsg = "Id thư mục Video sai, xem lại appsetting!";
                return new JsonResult(videoReturl);
            }

            if (TypeVideo == 0)
            {
                videoReturl.ErrMsg = "Type Video sai, xem lại appsetting!";
                return new JsonResult(videoReturl);
            }

            if (TimeOut == "0")
            {
                videoReturl.ErrMsg = "Timeout sai, xem lại appsetting!";
                return new JsonResult(videoReturl);
            }


            if (string.IsNullOrEmpty(VideoVirtual))
            {
                videoReturl.ErrMsg = "Thư mục ảo hóa sai, xem lại appsetting!";
                return new JsonResult(videoReturl);
            }

            //Kiểm tra kiểu ngày tháng
            if (videoConcatRequest.BeginDate == DateTime.MinValue && videoConcatRequest.EndDate == DateTime.MinValue)
            {
                videoReturl.ErrMsg = "Kiểu dữ liệu ngày tháng không hợp lệ!";
                return new JsonResult(videoReturl);
            }

            //Kiểm tra GID đã tồn tại hay chưa
            List<ConcatVideoCamera> gid = _iOTContext.ConcatVideoCameras.Where(x => x.Gid == videoConcatRequest.GID).ToList();
            if (gid.Count > 0)
            {
                var kqGID = gid.First();

                videoReturl.Id = kqGID.Id;
                videoReturl.UrlPath = kqGID.VideoUri;
                videoReturl.ErrMsg = "Đã tồn tại GID!";

                return new JsonResult(videoReturl);
            }

            //Kiểm tra video tương tự trên hệ thống hay chưa
            List<ConcatVideoCamera> data = _iOTContext.ConcatVideoCameras.Where(x => x.BeginDate <= videoConcatRequest.BeginDate.AddSeconds(5) && x.BeginDate >= videoConcatRequest.BeginDate.AddSeconds(-5) && x.EndDate <= videoConcatRequest.EndDate.AddSeconds(5) && x.EndDate >= videoConcatRequest.EndDate.AddSeconds(-5) && x.CameraId == videoConcatRequest.CameraId).ToList();
            if (data.Count > 0)
            {
                var video = data.First();
                videoReturl.Id = video.Id;
                videoReturl.UrlPath = video.VideoUri;
                videoReturl.ErrMsg = "Đã tồn tại video trên hệ thống!";

                return new JsonResult(videoReturl);
            }

            //Kiểm tra cameraId có tồn tại hay ko
            var cameras = CameraData.Cameras
                   .Where(x => x.CameraId == videoConcatRequest.CameraId && x.BusinessId == TypeVideo).ToList();
            if (cameras.Count == 0)
            {
                videoReturl.ErrMsg = "Camera không tồn tại trên hệ thống!";
                return new JsonResult(videoReturl);
            }

            //Kiểm tra có file phù hợp để ghép hay không
            try
            {
                long? ThuMucWSID = 0;
                string ThuMucDuongDan = string.Empty;
                var thuMuc = _xmhtService.TaoThuMuc(null, ThuMucVideoSave, DateTime.Now.ToString("yyyyMM"), ref ThuMucWSID, ref ThuMucDuongDan);
                var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc);
                var urlTxt = _xmhtService.P_ThuMuc_LayTheoID(null, ThuMucCmdDelete);

                long thuMucConId = 0;
                var idThuMucLuuLay = _xmhtService.P_ThuMuc_LayTheoThuMucCha(null, ThuMucVideoDelete, videoConcatRequest.CameraId.ToString(), ref thuMucConId);
                var urlLuuLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLuuLay);
                if (urlLuu != null && urlTxt != null && idThuMucLuuLay > 0 && urlLuuLay != null)
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

                                var DuongDanFileLuu = Path.Combine(urlLuu.DuongDan, fileName);
                                var DuongDanFileTXT = Path.Combine(urlTxt.DuongDan, fileNameTxt);

                                //Concat Video
                                await _workVideoService.ConcatVideo(videoConcatRequest.CameraId, urlLuuLay.DuongDan, DuongDanFileTXT, videoConcatRequest.BeginDate, videoConcatRequest.EndDate, DuongDanFileLuu, TimeOut);

                                //Check file tồn tại
                                if (System.IO.File.Exists(DuongDanFileLuu))
                                {
                                    //Update table ConcatVideoCamera
                                    var videoUri = $"/{VideoVirtual}/" + dateNow + "/" + fileName;
                                    _iOTService.P_ConcatVideoCamera_Update(kq, videoUri, 20);

                                    videoReturl.Id = kq;
                                    videoReturl.GID = videoConcatRequest.GID;
                                    videoReturl.UrlPath = videoUri;
                                    videoReturl.ErrMsg = "Tạo lệnh ghép thành công!";

                                    return new JsonResult(videoReturl);

                                }

                                videoReturl.ErrMsg = "File video không tồn tại!";
                                return new JsonResult(videoReturl);

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
            catch (Exception ex)
            {

                videoReturl.ErrMsg = ex.Message;
                return new JsonResult(videoReturl);
            }
        }

        [HttpPost("Image")]
        public async Task<JsonResult> PostImage([FromBody] ImageGetRequest imageRequest)
        {
            ImageReturn imageReturl = new();
            if (ThuMucImageDelete == 0 || ThuMucImageSave == 0)
            {
                imageReturl.ErrMsg = "Id thư mục Image sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            if (TypeImage == 0)
            {
                imageReturl.ErrMsg = "Type Image sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            if (TimeOut == "0")
            {
                imageReturl.ErrMsg = "Timeout sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            if (string.IsNullOrEmpty(ImageVirtual))
            {
                imageReturl.ErrMsg = "Thư mục ảo hóa sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            try
            {
                if (imageRequest.SaveImage == true)
                {
                    return await  _workImageService.WorkImageRequest(imageRequest, ThuMucImageSave, DateTime.Now.ToString("yyyyMM"), TypeImage, TimeOut, ImageVirtual);
                }
                else
                {
                    return await _workImageService.WorkImageRequest(imageRequest, ThuMucImageDelete, imageRequest.CameraId.ToString(), TypeImage, TimeOut, ImageVirtual);
                }


            }
            catch (Exception ex)
            {
                imageReturl.ErrMsg = ex.Message;
                return new JsonResult(imageReturl);
            }
        }

        [HttpPost("ImageFromVideo")]
        public async Task<JsonResult> PostImageFromVideo([FromBody] ImageFromVideoRequest imageRequest)
        {
            ImageReturn imageReturl = new();

            if (ThuMucImageDelete == 0 || ThuMucImageSave == 0)
            {
                imageReturl.ErrMsg = "Id thư mục Image sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            if (ThuMucVideoDelete == 0)
            {
                imageReturl.ErrMsg = "Thư mục videp sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            if (TypeImage == 0)
            {
                imageReturl.ErrMsg = "Type Image sai, xem lại appsetting!";
                return new JsonResult(imageReturl);
            }

            try
            {

                return await _workImageService.WorkImageFromVideoRequest(imageRequest, ThuMucVideoDelete , imageRequest.CameraId.ToString(), ThuMucImageDelete);

            }
            catch (Exception ex)
            {
                imageReturl.ErrMsg = ex.Message;
                return new JsonResult(imageReturl);
            }
        }
    }
}
