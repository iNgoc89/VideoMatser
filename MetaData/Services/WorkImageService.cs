﻿using MetaData.Context;
using MetaData.Data;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MetaData.Services
{
    public class WorkImageService
    {
        public XmhtService _xmhtService;
        public IOTContext _iOTContext;
        public IOTService _iOTService;
        public WorkVideoService _workVideoService;

        CameraData CameraData;
        public WorkImageService(XmhtService xmhtService, IOTContext iOTContext, IOTService iOTService, WorkVideoService workVideoService) 
        {
            _xmhtService = xmhtService;
            _iOTContext = iOTContext;
            _iOTService = iOTService;
            _workVideoService = workVideoService;

            CameraData = CameraData.getInstance();
            if (CameraData.Cameras.Count == 0)
            {
                CameraData.Cameras = _iOTService.GetCameras().ToList();
            }
            
        }
        public async Task<JsonResult> WorkImageRequest(ImageGetRequest imageRequest, long? thuMucCha, string tenThuMucCon, int typeImage, string timeOut, string thuMucVirtual)
        {
            ImageReturn imageReturl = new();

            long? ThuMucWSID = 0;
            string ThuMucDuongDan = string.Empty;
            var thuMuc = _xmhtService.TaoThuMuc(null, thuMucCha, tenThuMucCon, ref ThuMucWSID, ref ThuMucDuongDan);
            var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc);

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
                var cameras = CameraData.Cameras.Where(x => x.CameraId == imageRequest.CameraId && x.BusinessId == typeImage).ToList();

                if (cameras.Count > 0)
                {
                    var camera = cameras.First();

                    await _workVideoService.GetImage(camera.RtspUrl, urlImageSave, timeOut);

                    //Kiểm tra file đã ghi hay chưa
                    if (System.IO.File.Exists(urlImageSave))
                    {
                        //nếu có tham số mới crop
                        if (imageRequest.X >= 0 && imageRequest.Y >= 0 && imageRequest.Width > 0 && imageRequest.Height > 0)
                        {
                            await _workVideoService.CropImage(urlImageSave, urlImageSave, imageRequest.X, imageRequest.Y, imageRequest.Width, imageRequest.Height);
                        }

                        if (imageRequest.SaveImage == true)
                        {
                            imageReturl.ImageUrl = imageUri;
                            imageReturl.ErrMsg = "Tạo image thành công!";
                            return new JsonResult(imageReturl);
                        }

                        var base64Image = await _workVideoService.ImageToBase64(urlImageSave);
                        if (!string.IsNullOrEmpty(base64Image))
                        {
                            imageReturl.Base64 = base64Image;
                            imageReturl.ErrMsg = "Tạo base64 cho image thành công!";
                            return new JsonResult(imageReturl);
                        }

                        imageReturl.ErrMsg = "Lỗi tạo bas64Image!";
                        return new JsonResult(imageReturl);
                    }

                    imageReturl.ErrMsg = $"Lỗi file không tồn tại! {urlImageSave}" ;
                    return new JsonResult(imageReturl);
                }

                imageReturl.ErrMsg = "Camera không hợp lệ!";
                return new JsonResult(imageReturl);
            }


            imageReturl.ErrMsg = $"Đường dẫn lưu image không tồn tại! {thuMuc} - {urlLuu}";
            return new JsonResult(imageReturl);
        }

        public async Task<JsonResult> WorkImageFromVideoRequest(ImageFromVideoRequest imageRequest, long? thuMucCha, string tenThuMucCon, long? thuMucLuuAnh)
        {
            List<ImageReturn> imageReturls = new List<ImageReturn>();

            long? ThuMucWSID = 0;
            string ThuMucDuongDan = string.Empty;
            var thuMuc = _xmhtService.TaoThuMuc(null, thuMucCha, tenThuMucCon, ref ThuMucWSID, ref ThuMucDuongDan);
            var urlLuu = _xmhtService.P_ThuMuc_LayTheoID(null, thuMuc);


            var thuMucLuu = _xmhtService.TaoThuMuc(null, thuMucLuuAnh, imageRequest.CameraId.ToString(), ref ThuMucWSID, ref ThuMucDuongDan);
            var urlLuuAnh = _xmhtService.P_ThuMuc_LayTheoID(null, thuMucLuu);

            var fileName = imageRequest.GID + "_%d.jpg";

            if (thuMuc > 0 && urlLuu != null && urlLuuAnh != null)
            {
                var urlImageSave = Path.Combine(urlLuuAnh.DuongDan, fileName);

                //Tạo ảnh
                await _workVideoService.GetImageFromVideo(imageRequest.CameraId, urlLuu.DuongDan, imageRequest.BeginDate, imageRequest.EndDate, imageRequest.AnhTrenGiay, urlImageSave);

                //Lấy danh sách ảnh
                var images = await _workVideoService.FindFile(urlLuuAnh.DuongDan, imageRequest.GID.ToString(), imageRequest.X, imageRequest.Y, imageRequest.Width, imageRequest.Height);
                if (images?.Count > 0)
                {
                    imageReturls = images;
                    return new JsonResult(imageReturls);
                }
                else
                {
                    ImageReturn imageReturn = new ImageReturn();
                    //Kiểm tra cameraId
                    var cameras = CameraData.Cameras.Where(x => x.CameraId == imageRequest.CameraId).ToList();

                    if (cameras.Count > 0)
                    {
                        var camera = cameras.First();
                        var fileNameNoVideo = imageRequest.GID + ".jpg";
                        var urlImageSaveNoVideo = Path.Combine(urlLuuAnh.DuongDan, fileNameNoVideo);

                        await _workVideoService.GetImage(camera.RtspUrl, urlImageSaveNoVideo, "30000");
                    
                        //Kiểm tra file đã ghi hay chưa
                        if (System.IO.File.Exists(urlImageSaveNoVideo))
                        {
                            //nếu có tham số mới crop
                            if (imageRequest.X >=0 && imageRequest.Y >= 0 && imageRequest.Width > 0 && imageRequest.Height > 0)
                            {
                                await _workVideoService.CropImage(urlImageSaveNoVideo, urlImageSaveNoVideo, imageRequest.X, imageRequest.Y, imageRequest.Width, imageRequest.Height);
                            }
                           
                            var base64Image = await _workVideoService.ImageToBase64(urlImageSaveNoVideo);
                            if (!string.IsNullOrEmpty(base64Image))
                            {
                                
                                imageReturn.Base64 = base64Image;
                                imageReturn.ErrMsg = "Tạo base64 cho image thành công!";
                                return new JsonResult(imageReturn);
                            }

                            imageReturn.ErrMsg = "Lỗi tạo bas64Image!";
                            return new JsonResult(imageReturn);
                        }

                        imageReturn.ErrMsg = $"Lỗi file không tồn tại! {urlImageSave}";
                        return new JsonResult(imageReturn);
                    }

                    imageReturn.ErrMsg = "Camera không hợp lệ!";
                    return new JsonResult(imageReturn);
                }   

            }

            return new JsonResult(imageReturls);
        }
    }
}
