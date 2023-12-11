﻿using MetaData.Context;
using MetaData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MetaData.Services
{
    public class WorkImageService
    {
        public XmhtService _xmhtService;
        public IOTContext _iOTContext;
        public WorkVideoService _workVideoService;
        public WorkImageService(XmhtService xmhtService, IOTContext iOTContext, WorkVideoService workVideoService) 
        {
            _xmhtService = xmhtService;
            _iOTContext = iOTContext;
            _workVideoService = workVideoService;
        }
        public async Task<JsonResult> WorkImageRequest(ImageGetRequest imageRequest, long? thuMucCha, string tenThuMucCon, int typeImage, string timeOut, string thuMucVirtual)
        {
            ImageReturn imageReturl = new();

            long? ThuMucWSID = 0;
            string ThuMucDuongDan = string.Empty;
            var thuMuc = _xmhtService.TaoThuMuc(null, thuMucCha, tenThuMucCon, ref ThuMucWSID, ref ThuMucDuongDan);
            var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc).Result;

            var fileName = imageRequest.GID + ".jpg";
            var imageUri = $"/{thuMucVirtual}/" + tenThuMucCon + "/" + fileName;

            if (thuMuc > 0 && urlLuu != null)
            {
                //kiểm tra GID
                var urlImageSave = Path.Combine(urlLuu.DuongDan, fileName);

                if (System.IO.File.Exists(urlImageSave))
                {
                    if (imageRequest.SaveImage == true)
                    {
                        imageReturl.ImageUrl = imageUri;
                        imageReturl.ErrMsg = "Đã tồn tại image!";
                        return new JsonResult(imageReturl);
                    }

                    imageReturl.ErrMsg = "Image đã convert bas64Image trước đó!";
                    return new JsonResult(imageReturl);
                }

                //Kiểm tra cameraId
                var cameras = _iOTContext.CameraBusinesses.Include(x => x.Camera)
                    .Where(x => x.CameraId == imageRequest.CameraId && x.BusinessId == typeImage && x.IsActive == true).ToList();
                if (cameras.Count > 0)
                {
                    var camera = cameras.First();

                    await _workVideoService.GetImage(camera.Camera.RtspUrl, urlImageSave, timeOut);

                    //Kiểm tra file đã ghi hay chưa
                    if (System.IO.File.Exists(urlImageSave))
                    {
                        if (imageRequest.SaveImage == true)
                        {
                            imageReturl.ImageUrl = imageUri;
                            imageReturl.ErrMsg = "Tạo image thành công!";
                            return new JsonResult(imageReturl);
                        }

                        var base64Image = _workVideoService.ImageToBase64(urlImageSave);
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

                imageReturl.ErrMsg = "Camera không hợp lệ!";
                return new JsonResult(imageReturl);
            }


            imageReturl.ErrMsg = "Đường dẫn lưu image không tồn tại!";
            return new JsonResult(imageReturl);
        }
    }
}