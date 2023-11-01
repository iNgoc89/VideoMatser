﻿using FFmpegWebAPI.Data;
using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.Design;

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
        public static string? ThuMucLay = string.Empty;
        public static string? ThuMucLuu = string.Empty;
        public long? ThuMucId = null;
        public VideoController(IOTContext iOTContext, IOTService iOTService, WorkVideoService workVideoService, XmhtService xmhtService, IConfiguration configuration)
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _workVideoService = workVideoService;
            _xmhtService = xmhtService;
            _configuration = configuration;

            ThuMucLay = _configuration["ThuMucNghiepVu:VideoCamera"];
            ThuMucLuu = _configuration["ThuMucNghiepVu:ConcatVideoCamera"];
        }

        // GET api/<VideoController>/GID

        [HttpGet("{GID}")]
        public ConcatVideoCamera? Get(Guid GID)
        {
            var data = _iOTContext?.ConcatVideoCameras?.Where(x => x.GID == GID)?.FirstOrDefault();
            return data;
        }

        // POST api/<VideoController>

        [HttpPost]
        public VideoReturl Post(Guid GID, int cameraId, string beginDate, string endDate)
        {
            VideoReturl videoReturl = new();
            int kq = 0;
            DateTime.TryParse(beginDate, out DateTime BD);
            DateTime.TryParse(endDate, out DateTime ED);

            if (BD > DateTime.MinValue && ED > DateTime.MinValue)
            { 
                //Kiểm tra video tương tự trên hệ thống hay chưa
                List<ConcatVideoCamera> data = _iOTContext.ConcatVideoCameras.Where(x => x.BeginDate <= BD.AddSeconds(5) && x.BeginDate >= BD.AddSeconds(-5) && x.EndDate <= ED.AddSeconds(5) && x.EndDate >= ED.AddSeconds(-5)).ToList();
                if (data.Count > 0)
                {
                    var video = data.First();
                    videoReturl.Id = video.Id;
                    videoReturl.UrlPath = video.VideoUri;
                    videoReturl.ErrMsg = "Đã tồn tại video trên hệ thống!";

                    return videoReturl;
                }

                //Kiểm tra có file phù hợp để ghép hay không
                long idThuMucLay = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucLay);
                long idThuMucLuu = _xmhtService.P_ThuMuc_LayTMNgiepVu(null, ref ThuMucId, ThuMucLuu);
                if (idThuMucLay > 0 && idThuMucLuu > 0)
                {
                    var urlLay = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLay).Result;
                    var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, idThuMucLuu).Result;
                    if (urlLay != null && urlLuu != null)
                    {
                        var checkFile = _workVideoService.CheckFile(cameraId, urlLay.DuongDan, BD, ED);
                        if (checkFile?.Length > 0)
                        {
                            kq = _iOTService.P_ConcatVideoCamera_Insert(GID, cameraId, BD, ED);
                            if (kq > 0)
                            {
                                var dateNow = DateTime.Now.ToString("yyyyMM");
                                videoReturl.Id = kq;
                                videoReturl.UrlPath = $@"~\{urlLuu.Ten}\{dateNow}\{GID}.mp4";
                                videoReturl.ErrMsg = "Ghép video thành công!";

                                return videoReturl;
                            }
                          

                        }
                        else
                        {
                            videoReturl.Id = 0;
                            videoReturl.UrlPath = null;
                            videoReturl.ErrMsg = "Không có video phù hợp. Xem lại thời điểm lấy video!";
                        }
                    }

                  

                }
              

               
            }

            return videoReturl;
        }


    }
}
