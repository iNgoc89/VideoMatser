# Workflow and Architecture

## 1. Tong quan

Du an xu ly du lieu camera bang FFmpeg, gom 3 thanh phan chinh:

- `FFmpegWebAPI`: ASP.NET Core Web API de nhan yeu cau ghep video, chup anh, trich anh tu video.
- `WorkerVideoCameraService`: .NET Worker Service chay nen/Windows Service, lien tuc lay stream RTSP tu camera va cat thanh cac file video ngan.
- `MetaData`: thu vien dung chung, chua EF Core context, model, middleware API key, service truy cap DB, service FFmpeg va service xu ly file.

Luon du lieu tong quat:

1. Worker doc danh sach camera dang chay tu database IOT.
2. Worker dung FFmpeg lay RTSP va ghi video segment theo tung camera vao thu muc raw video.
3. API nhan yeu cau theo `GID`, `CameraId`, khoang thoi gian hoac tham so anh.
4. API tra cuu thu muc trong XMHT, tim raw video/raw image phu hop, goi FFmpeg de ghep/cat/chup.
5. Ket qua duoc luu vao thu muc nghiep vu va tra ve URL ao hoac base64.
6. Cac job delete cua worker xoa file tam sau thoi gian cau hinh.

## 2. Cau truc solution

Solution goc: `FFmpegWebAPI.sln`

```
FFmpegWebAPI.sln
FFmpegWebAPI/
  FFmpegWebAPI.csproj
  Program.cs
  Controllers/VideoController.cs
  appsettings.Development.json
WorkerVideoCameraService/
  WorkerVideoCameraService.csproj
  Program.cs
  Services/
    ScopedVideoCameraService.cs
    IScopedVideoCameraService.cs
    DeleteVideoCameraService.cs
    DeleteImageCameraService.cs
    DeleteTxtService.cs
MetaData/
  MetaData.csproj
  Context/
  Data/CameraData.cs
  Models/
  Services/
```

## 3. Cong nghe va package

- Target framework: `.NET 8.0`.
- Web API: ASP.NET Core MVC controller, Swagger trong Development.
- Worker: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Hosting.WindowsServices`.
- Database: SQL Server.
- ORM/truy cap DB:
  - EF Core cho `IOTContext` va entity scaffold.
  - Dapper cho stored procedure/function nghiep vu.
- Video/image engine: FFmpeg.
- Security API: middleware custom doc header `Api_Key`.

## 4. Project FFmpegWebAPI

### 4.1 Program

File: `FFmpegWebAPI/Program.cs`

Dang ky:

- Controllers, JSON giu nguyen ten property (`PropertyNamingPolicy = null`).
- Swagger/OpenAPI trong Development.
- `IOTContext` dung connection string `IOTConnection`.
- Scoped services:
  - `IOTService`
  - `WorkVideoService`
  - `XmhtService`
  - `WorkImageService`
- Middleware `CustomApiKeyService`.
- Static files, HTTPS redirection, authorization, map controllers.

### 4.2 VideoController

Route goc: `api/Video`

Controller doc cac cau hinh:

- `ThuMucNghiepVu:VideoDelete`
- `ThuMucNghiepVu:VideoSave`
- `ThuMucNghiepVu:CmdDelete`
- `ThuMucNghiepVu:ImageSave`
- `ThuMucNghiepVu:ImageDelete`
- `ThuMucNghiepVu:VideoVirtual`
- `ThuMucNghiepVu:ImageVirtual`
- `TypeCamera:TypeVideo`
- `TypeCamera:TypeImage`
- `TimeOutFFmpeg:Millisecond`
- `TimeVideo`
- `FFmpeg:Url`

Controller co cache danh sach camera qua singleton `CameraData`.

#### GET `api/Video/{GID}`

Muc dich:

- Lay ban ghi `ConcatVideoCamera` theo `GID`.

Ket qua:

- Co data: tra danh sach ban ghi.
- Khong co: tra chuoi `"GID khong ton tai!"`.
- `Guid.Empty`: `NoContent`.

#### POST `api/Video/ConcatVideo`

Request model: `VideoConcatRequest`

```json
{
  "GID": "guid",
  "CameraId": 1,
  "BeginDate": "2026-06-16T10:00:00",
  "EndDate": "2026-06-16T10:05:00"
}
```

Workflow:

1. Validate cau hinh thu muc, type video, timeout, virtual path.
2. Validate `BeginDate`, `EndDate`.
3. Kiem tra trung `GID` bang `cmrs.CheckGID`.
4. Kiem tra video cung khoang thoi gian/camera bang `cmrs.CheckVideo`.
5. Kiem tra `CameraId` ton tai va co `BusinessId == TypeVideo`.
6. Tao thu muc luu video ghep theo thang `yyyyMM`.
7. Lay thu muc command txt va thu muc raw video theo camera.
8. Tim cac file raw video co ten dang `{CameraId}_{Ticks}.mp4` va ticks nam trong khoang:
   - `beginDate - TimeVideo <= ticks < endDate`
9. Insert ban ghi concat bang `cmrs.P_ConcatVideoCamera_Insert`.
10. Tao file txt concat FFmpeg.
11. Chay FFmpeg concat.
12. Neu file output ton tai, update DB bang `cmrs.P_ConcatVideoCamera_Update` voi status `20`.
13. Tra ve `VideoReturl` gom `Id`, `GID`, `UrlPath`, `ErrMsg`.

Output file:

- Ten file: `{GID}.mp4`
- URL ao: `/{VideoVirtual}/{yyyyMM}/{GID}.mp4`

#### POST `api/Video/Image`

Request model: `ImageGetRequest`

```json
{
  "GID": "guid",
  "CameraId": 1,
  "SaveImage": true,
  "Resize": true,
  "X": 0,
  "Y": 0,
  "Width": 640,
  "Height": 480
}
```

Workflow:

1. Validate cau hinh image, type image, timeout, virtual path.
2. Neu `SaveImage == true`:
   - Luu anh vao thu muc `ImageSave/yyyyMM`.
   - Tra ve `ImageUrl`.
3. Neu `SaveImage == false`:
   - Luu anh tam vao thu muc `ImageDelete/{CameraId}`.
   - Doc file va tra ve `Base64`.
4. Lay camera co `BusinessId == TypeImage`.
5. Goi FFmpeg chup 1 frame tu RTSP.
6. Neu co tham so crop hop le (`X >= 0`, `Y >= 0`, `Width > 0`, `Height > 0`) thi crop bang FFmpeg.

Output file:

- Ten file: `{GID}.jpg`
- URL ao neu save: `/{ImageVirtual}/{folder}/{GID}.jpg`

#### POST `api/Video/ImageFromVideo`

Request model: `ImageFromVideoRequest`

```json
{
  "GID": "guid",
  "CameraId": 1,
  "AnhTrenGiay": 2,
  "BeginDate": "2026-06-16T10:00:00",
  "EndDate": "2026-06-16T10:05:00",
  "X": 0,
  "Y": 0,
  "Width": 640,
  "Height": 480
}
```

Workflow:

1. Validate thu muc image/video va type image.
2. Tim raw video cua camera trong khoang thoi gian.
3. Dung FFmpeg trich anh tu video voi filter `fps=1/{AnhTrenGiay}`.
4. Anh tao ra co ten `{GID}_%d.jpg`.
5. Tim anh theo prefix `{GID}`, crop neu co tham so crop.
6. Tra ve danh sach `ImageReturn` dang base64.
7. Neu khong tim thay video, fallback chup anh truc tiep tu RTSP va tra ve 1 anh base64.

## 5. Project WorkerVideoCameraService

### 5.1 Program

File: `WorkerVideoCameraService/Program.cs`

Dang ky hosted service:

- `ScopedVideoCameraService`: job lay video tu camera.
- `DeleteVideoCameraService`: xoa raw video tam.
- `DeleteTxtService`: xoa file txt command concat.
- `DeleteImageCameraService`: xoa raw image/image tam.

Dang ky scoped service:

- `IScopedVideoCameraService -> ScopedProcessingService`
- `IDeleteVideoCameraService -> DeleteProcessingService`
- `IDeleteTxtService -> DeleteTxtProcessingService`
- `IDeleteImageCameraService -> DeleteImageProcessingService`
- `XmhtService`
- `WorkVideoService`
- `WorkDeleteService`
- `IOTService`
- `IOTContext`

Worker duoc cau hinh de chay nhu Windows Service bang `services.AddWindowsService()`.

### 5.2 ScopedVideoCameraService

Day la hosted service wrapper. Khi start:

1. Tao DI scope.
2. Lay `IScopedVideoCameraService`.
3. Goi `RunApp`.

Khi stop:

1. Goi `StopApp`.
2. `StopApp` cho cac task hien tai hoan thanh.
3. Kill cac FFmpeg process con dang chay.
4. Goi `WorkVideoService.StopProcess()` de terminate Windows Job Object.

### 5.3 ScopedProcessingService

Day la job lay video chinh.

Cau hinh doc:

- `FFmpeg:Url`
- `TypeCamera:TypeVideo`
- `ThuMucNghiepVu:VideoDelete`
- `TimeOutFFmpeg:Millisecond`
- `TimeVideo`
- `TimeProcess`

Workflow:

1. Validate `VideoDelete`, `TimeOut`, `TypeVideo`, `TimeVideo`, `TimeProcess`.
2. Load danh sach camera video tu `IOTService.GetCameras()`, loc `BusinessId == TypeVideo`.
3. Lap vo han den khi cancellation requested.
4. Moi vong lap lay camera dang chay tu `cmrs.GetCameraVideo_DangChay()`.
5. Voi moi camera dang chay:
   - Tao/lay thu muc con theo `CameraId` duoi thu muc raw video.
   - Tao file `{CameraId}_{DateTime.Now.Ticks}.mp4`.
   - Goi `WorkVideoService.GetVideo` de cat RTSP thanh segment.
   - Delay giua tung camera theo `TimeProcess`.
6. Sau khi lap het camera, delay phan thoi gian con lai cua chu ky `TimeVideo`.

Y nghia tham so:

- `TimeVideo`: do dai segment video theo millisecond.
- `TimeProcess`: thoi gian gian cach khi start task giua cac camera, tranh start dong loat.
- `TimeOutFFmpeg:Millisecond`: timeout truyen cho FFmpeg khi doc RTSP.

### 5.4 Cac job delete

Moi job lay ID thu muc cha tu config, sau do goi `WorkDeleteService.DeleteFiles`.

- `DeleteVideoCameraService`: xoa file trong `ThuMucNghiepVu:VideoDelete`.
- `DeleteImageCameraService`: xoa file trong `ThuMucNghiepVu:ImageDelete`.
- `DeleteTxtService`: xoa file trong `ThuMucNghiepVu:CmdDelete`.

`WorkDeleteService.DeleteFiles`:

1. Lay duong dan vat ly tu DB XMHT bang `apps.p_ThuMuc_LayTheoID`.
2. Moi 1 giay quet tat ca file con.
3. Neu `File.GetCreationTime(file) < DateTime.Now.AddMinutes(-TimeDelete)` thi xoa.
4. Chi xoa neu file khong bi lock.

## 6. Project MetaData

### 6.1 Data/CameraData

Singleton nho trong process:

- `List<CameraModel> Cameras`: cache danh sach camera.
- `List<Process> ffmpegProcesses`: danh sach FFmpeg/CMD process dang chay.

Luu y: singleton nay chi song trong tung process rieng. API va Worker khong chia se memory voi nhau.

### 6.2 IOTContext

EF Core context scaffold tu DB IOT.

Entities:

- `Business` -> schema `cmrs`, table `Business`.
- `Camera` -> schema `cmrs`, table `Camera`.
- `CameraBusiness` -> schema `cmrs`, table `CameraBusiness`.
- `ConcatVideoCamera` -> schema `cmrs`, table `ConcatVideoCamera`.

`ConcatVideoCamera` co cac truong chinh:

- `Id`
- `Gid`
- `BeginDate`
- `EndDate`
- `VideoUri`
- `Status`
- `CameraId`

### 6.3 IOTService

Dung Dapper voi connection string `IOTConnection`.

Stored procedure/function dang dung:

- `cmrs.P_ConcatVideoCamera_Insert`
- `cmrs.P_ConcatVideoCamera_Update`
- `cmrs.P_ConcatVideoCamera_UpdateStatus`
- `cmrs.GetCameraData()`
- `cmrs.GetCameraVideo_DangChay()`
- `cmrs.CheckGID(gid)`
- `cmrs.CheckVideo(beginDate, endDate, camId)`

### 6.4 XmhtService

Dung Dapper voi connection string `XMHTConnection`.

Stored procedure dang dung:

- `apps.p_ThuMuc_LayTheoID`
- `apps.p_ThuMuc_Them1`
- `apps.p_ThuMuc_LayTheoThuMucCha`

Trach nhiem:

- Lay duong dan vat ly cua thu muc nghiep vu.
- Tao thu muc vat ly neu chua co.
- Ghi/lay metadata thu muc trong DB XMHT.

### 6.5 WorkVideoService

Service trung tam cho FFmpeg/file.

Chuc nang:

- `GetVideo`: ghi segment video tu RTSP.
- `CreateConcatTxt`: tao file txt danh sach input cho FFmpeg concat.
- `ConcatVideo`: ghep nhieu segment thanh MP4.
- `CheckFile`: tim file video theo camera va khoang thoi gian.
- `GetImage`: chup 1 frame tu RTSP.
- `GetImageFromVideo`: trich nhieu anh tu video.
- `CropImage`: crop anh bang FFmpeg.
- `FindFile`: tim anh output, crop neu can, convert base64.
- `ImageToBase64`: doc file anh thanh base64.
- `DeleteFile`: xoa file neu khong bi lock.
- `StopProcess`: terminate Windows Job Object.

FFmpeg command chinh:

- Lay video:

```text
ffmpeg -hwaccel cuda -hwaccel_output_format cuda -t {seconds} -rtsp_transport tcp -timeout {timeOut} -i {rtspUrl} -c:v h264_nvenc -an -vf scale_cuda=640:480 -r 25 -maxrate 1M -bufsize 2M {output} -y -loglevel quiet -hide_banner
```

- Ghep video:

```text
ffmpeg -f concat -safe 0 -i {txtFileConcat} -c copy {output} -timeout {timeOut}
```

- Chup anh:

```text
ffmpeg -rtsp_transport tcp -xerror -timeout {timeOut} -i "{rtspUrl}" -vf scale=640:480 -frames:v 1 {output} -y -loglevel quiet -an -hide_banner
```

- Crop:

```text
ffmpeg -i {sourcePath} -vf crop={width}:{height}:{x}:{y} {outputPath} -y -loglevel quiet -an -hide_banner
```

Quan ly process:

- `RunFFmpegProcess` va `RunCMDProcess` them process vao `CameraData.ffmpegProcesses`.
- Process duoc assign vao Windows Job Object `VideoService`.
- Job Object dat flag `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, giup dong child process khi service dung.

### 6.6 WorkImageService

Service orchestration cho API image.

- `WorkImageRequest` xu ly `POST api/Video/Image`.
- `WorkImageFromVideoRequest` xu ly `POST api/Video/ImageFromVideo`.

### 6.7 WorkDeleteService

Service xoa file tam theo tuoi file.

### 6.8 CustomApiKeyService

Middleware kiem tra header:

```text
Api_Key: {configured_api_key}
```

Neu thieu hoac sai:

- HTTP 401.
- Message tieng Viet ve API key khong hop le/khong chinh xac.

## 7. Model request/response

### CameraModel

- `BusinessId`
- `CameraId`
- `Code`
- `Description`
- `Name`
- `RtspUrl`
- `Type`

### VideoConcatRequest

- `GID`: ma request duy nhat.
- `CameraId`: camera can ghep.
- `BeginDate`: thoi diem bat dau.
- `EndDate`: thoi diem ket thuc.

### VideoReturl

- `Id`: ID ban ghi concat.
- `GID`
- `UrlPath`: URL ao den file video.
- `ErrMsg`: thong bao ket qua/loi.

### ImageGetRequest

- `GID`
- `CameraId`
- `SaveImage`: true de luu lau dai va tra URL, false de tra base64 tu file tam.
- `Resize`: mac dinh true.
- `X`, `Y`, `Width`, `Height`: crop optional.

### ImageFromVideoRequest

- `GID`
- `CameraId`
- `AnhTrenGiay`: khoang fps filter dang dung la `fps=1/{AnhTrenGiay}`.
- `BeginDate`, `EndDate`: optional; neu thieu thi lay khoang gan hien tai.
- `X`, `Y`, `Width`, `Height`: crop optional.

### ImageReturn

- `Base64`
- `ImageUrl`
- `ErrMsg`

### ThuMuc

- `ThuMucID`
- `ThuMucChaID`
- `Ten`
- `DuongDan`

## 8. Cau hinh

Cac file cau hinh hien co:

- `FFmpegWebAPI/appsettings.Development.json`
- `WorkerVideoCameraService/appsettings.json`
- `WorkerVideoCameraService/appsettings.Development.json`

Khuyen nghi khong commit secret production. Cac khoa nhu password SQL va API key nen duoc dua vao environment variables, user-secrets, secret store hoac config rieng ngoai source control.

### ConnectionStrings

- `IOTConnection`: ket noi DB IOT, dung cho EF Core va `IOTService`.
- `XMHTConnection`: ket noi DB XMHT, dung cho `XmhtService`.

### FFmpeg

- `FFmpeg:Url`: duong dan file `ffmpeg.exe`.

Luu y hien tai:

- Mot so command goi truc tiep `ffmpeg` qua `CMD.exe`, khong dung `FFmpeg:Url`.
- May chay service can co FFmpeg trong `PATH` hoac can sua command de dung duong dan cau hinh.

### TypeCamera

- `TypeVideo`: BusinessId dung cho luong video.
- `TypeImage`: BusinessId dung cho luong chup anh.

### TimeDelete

- `TimeDelete:Time`: so phut giu file tam truoc khi job delete xoa.

### TimeOutFFmpeg

- `TimeOutFFmpeg:Millisecond`: timeout truyen cho FFmpeg khi doc RTSP.

### TimeVideo

- Do dai moi segment video, tinh bang millisecond.
- Worker chuyen sang giay khi truyen vao FFmpeg `-t`.
- API dung gia tri nay de noi rong khoang tim file raw video ve phia truoc `BeginDate`.

### TimeProcess

- Delay giua luc start task ghi video cho tung camera, tinh bang millisecond.

### Api_Key

- Gia tri so sanh voi request header `Api_Key`.

### ThuMucNghiepVu

- `VideoDelete`: ID thu muc raw video, worker ghi segment vao day va job delete xoa file cu.
- `VideoSave`: ID thu muc luu video da ghep.
- `CmdDelete`: ID thu muc luu file txt concat tam.
- `ImageSave`: ID thu muc luu anh lau dai.
- `ImageDelete`: ID thu muc luu anh tam/raw image.
- `VideoVirtual`: alias URL ao cho video output.
- `ImageVirtual`: alias URL ao cho image output.

## 9. Quy uoc file va thu muc

### Raw video segment

Thu muc:

```text
{VideoDelete physical path}/{CameraId}/
```

Ten file:

```text
{CameraId}_{DateTime.Now.Ticks}.mp4
```

Y nghia:

- `CameraId`: camera source.
- `Ticks`: timestamp duoc dung de tim file theo khoang thoi gian.

### Video da ghep

Thu muc:

```text
{VideoSave physical path}/{yyyyMM}/
```

Ten file:

```text
{GID}.mp4
```

URL:

```text
/{VideoVirtual}/{yyyyMM}/{GID}.mp4
```

### File txt concat

Thu muc:

```text
{CmdDelete physical path}/
```

Ten file:

```text
{GID}.txt
```

Noi dung duoc tao theo format FFmpeg concat:

```text
file 'path-to-segment-1'
file 'path-to-segment-2'
```

### Anh chup/luu

Ten file:

```text
{GID}.jpg
```

Neu trich tu video:

```text
{GID}_%d.jpg
```

## 10. Database contract

Du an phu thuoc cac schema/stored procedure/function trong SQL Server.

### IOT database

Schema chinh: `cmrs`

Bang/entity:

- `cmrs.Business`
- `cmrs.Camera`
- `cmrs.CameraBusiness`
- `cmrs.ConcatVideoCamera`

Function/procedure:

- `cmrs.GetCameraData()`
- `cmrs.GetCameraVideo_DangChay()`
- `cmrs.CheckGID(@GID)`
- `cmrs.CheckVideo(@BeginDate, @EndDate, @CameraId)`
- `cmrs.P_ConcatVideoCamera_Insert`
- `cmrs.P_ConcatVideoCamera_Update`
- `cmrs.P_ConcatVideoCamera_UpdateStatus`

### XMHT database

Schema chinh: `apps`

Procedure:

- `apps.p_ThuMuc_LayTheoID`
- `apps.p_ThuMuc_Them1`
- `apps.p_ThuMuc_LayTheoThuMucCha`

Yeu cau du lieu:

- Cac ID trong `ThuMucNghiepVu` phai ton tai trong DB XMHT.
- `DuongDan` cua thu muc cha phai ton tai tren file system may chay API/Worker.
- Tai khoan chay service phai co quyen read/write/delete tuong ung.

## 11. Security

Hien trang:

- API dung API key custom qua header `Api_Key`.
- Static files duoc enable.
- Swagger chi enable trong Development.

Rui ro/can luu y:

- Connection string va API key dang xuat hien trong appsettings. Nen rotate secret neu repo da chia se.
- Middleware API key khong bo qua Swagger/static file; moi request deu can header API key sau khi middleware duoc gan.
- Dapper co mot so cau query noi chuoi trong `CheckGID` va `CheckVideo`; nen tham so hoa de giam rui ro SQL injection/format date sai culture.

## 12. Cach chay local

### Build solution

```powershell
dotnet build .\FFmpegWebAPI.sln
```

### Chay API

```powershell
dotnet run --project .\FFmpegWebAPI\FFmpegWebAPI.csproj
```

Development URL theo launchSettings:

- HTTP: `http://localhost:5146`
- HTTPS: `https://localhost:7219`
- Swagger: `/swagger`

### Chay Worker

```powershell
dotnet run --project .\WorkerVideoCameraService\WorkerVideoCameraService.csproj
```

### Goi API mau

Can co header:

```text
Api_Key: {configured_api_key}
```

Health check API khong yeu cau API key:

```powershell
Invoke-RestMethod -Uri "http://localhost:5146/health"
```

Health check chi tiet yeu cau API key:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:5146/health/detail" `
  -Headers @{ Api_Key = "<api-key>" }
```

Concat video:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5146/api/Video/ConcatVideo" `
  -Headers @{ Api_Key = "<api-key>" } `
  -ContentType "application/json" `
  -Body '{"GID":"00000000-0000-0000-0000-000000000001","CameraId":1,"BeginDate":"2026-06-16T10:00:00","EndDate":"2026-06-16T10:05:00"}'
```

Chup anh:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5146/api/Video/Image" `
  -Headers @{ Api_Key = "<api-key>" } `
  -ContentType "application/json" `
  -Body '{"GID":"00000000-0000-0000-0000-000000000002","CameraId":1,"SaveImage":true,"Resize":true}'
```

## 13. Trien khai Windows Service

Huong tiep can lam ro tuy moi truong, nhung service da co `AddWindowsService()`.

Quy trinh goi y:

1. Publish worker:

```powershell
dotnet publish .\WorkerVideoCameraService\WorkerVideoCameraService.csproj -c Release -o C:\Services\WorkerVideoCameraService
```

2. Tao Windows Service:

```powershell
sc.exe create WorkerVideoCameraService binPath= "C:\Services\WorkerVideoCameraService\WorkerVideoCameraService.exe" start= auto
```

3. Dam bao account service co quyen:

- Doc connection string/secret.
- Chay FFmpeg.
- Read/write/delete cac thu muc trong XMHT.
- Truy cap SQL Server.

4. Start service:

```powershell
sc.exe start WorkerVideoCameraService
```

## 14. Task van hanh chinh

### Worker raw video

- Muc dich: tao nguon raw video lien tuc cho moi camera dang chay.
- Input: danh sach camera tu `cmrs.GetCameraVideo_DangChay()`.
- Output: segment `.mp4` trong `VideoDelete/{CameraId}`.
- Chu ky: theo `TimeVideo`, co offset giua camera theo `TimeProcess`.

### API concat video

- Muc dich: cat/ghep video theo khoang thoi gian nguoi dung yeu cau.
- Input: `GID`, `CameraId`, `BeginDate`, `EndDate`.
- Output: file `.mp4`, URL ao, ban ghi DB.

### API chup anh

- Muc dich: lay anh hien tai tu camera.
- Input: `GID`, `CameraId`, `SaveImage`, crop optional.
- Output: URL anh hoac base64.

### API trich anh tu video

- Muc dich: lay anh tu raw video theo khoang thoi gian.
- Input: `GID`, `CameraId`, `AnhTrenGiay`, `BeginDate`, `EndDate`, crop optional.
- Output: danh sach base64; fallback chup RTSP neu khong co video.

### Delete raw video/image/txt

- Muc dich: don file tam.
- Input: thu muc nghiep vu va `TimeDelete`.
- Output: file cu bi xoa neu khong bi lock.

## 15. Cac diem can cai thien

1. Dua secret ra khoi appsettings.
2. Tham so hoa cac query Dapper dang noi chuoi (`CheckGID`, `CheckVideo`).
3. Thong nhat cach goi FFmpeg:
   - Hien co cho config `FFmpeg:Url`, nhung nhieu command goi `ffmpeg` truc tiep qua CMD.
4. Bo `ffmpeg` parameter khong dung trong `WorkVideoService.GetVideo`.
5. Da trien khai: kiem soat so luong task trong `ScopedProcessingService`:
   - Task ghi video duoc dua vao danh sach active co lock.
   - Moi chu ky se remove task da completed va cap nhat `WorkerHealthState`.
   - Khi stop service chi await snapshot task con dang active.
6. Them logging chi tiet cho cac catch dang rong trong delete service.
7. Chuan hoa tieng Viet co dau/encoding trong appsettings comment.
8. Xem lai viec `File.Open(file, FileAccess.Write)` trong `CheckFile` va `FindFile`; muc dich chi de check lock/doc file nen co the gay loi quyen khong can thiet.
9. Da trien khai: them health check cho API/Worker:
   - API expose `GET /health`, bo qua middleware API key, chi tra trang thai tong quat.
   - API expose `GET /health/detail`, yeu cau API key, tra JSON chi tiet tung check.
   - API check ket noi `IOTConnection`, `XMHTConnection`, thu muc nghiep vu va FFmpeg.
   - Worker dang ky `DatabaseHealthCheck`, `StorageFolderHealthCheck`, `FFmpegHealthCheck`, `WorkerCameraCaptureHealthCheck` va `WorkerHealthLogService`.
   - Worker log warning moi 30 giay neu health status khong healthy.
10. Them test cho logic tim file theo ticks, tao concat txt, validate request.
11. Xem lai CUDA/NVIDIA dependency:
    - Command lay video dung `-hwaccel cuda`, `h264_nvenc`, `scale_cuda`.
    - May chay service can GPU/driver/NVENC phu hop, hoac can fallback CPU.
12. Xem lai static file mapping de URL ao `VideoVirtual`/`ImageVirtual` map dung thu muc vat ly tren web server/IIS.
13. Da trien khai: Job Object trong `WorkVideoService` khong con lam fail request API neu server/IIS khong cho tao Job Object.
    - Tao Job Object bang ten unique theo process/request.
    - Neu tao/thiet lap Job Object that bai, service log warning va van cho FFmpeg/CMD chay tiep.
    - Process chi duoc assign vao Job Object khi handle hop le.
    - Handle duoc dong khi scope DI dispose.

## 16. Checklist cau hinh moi truong moi

- Cai .NET 8 Hosting Bundle/Runtime.
- Cai FFmpeg va dam bao path dung.
- Neu dung command CUDA, cai NVIDIA driver/NVENC phu hop.
- Tao/cap quyen cac thu muc vat ly trong XMHT.
- Tao metadata thu muc trong DB XMHT va cap nhat ID vao `ThuMucNghiepVu`.
- Dam bao DB IOT co camera, RTSP URL va mapping business type.
- Dam bao function/procedure `cmrs` va `apps` ton tai.
- Dat secret connection string va API key.
- Chay `dotnet build`.
- Chay API va test Swagger/API key.
- Chay Worker va kiem tra raw video segment duoc sinh ra.
- Test concat video theo khoang co segment.
- Test chup anh luu URL va base64.
- Test delete file tam sau `TimeDelete`.
