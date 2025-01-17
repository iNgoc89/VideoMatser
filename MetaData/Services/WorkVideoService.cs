﻿using MetaData.Context;
using MetaData.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Logging;
using MetaData.Data;
using System.Runtime.InteropServices;

namespace MetaData.Services
{
    public class WorkVideoService
    {
        public IOTContext _iOTContext;
        public string txtCmdConcat = string.Empty;
        public string CMD = "CMD.exe";
        private readonly ILogger<WorkVideoService> _logger;

        CameraData CameraData;

        //Gom các process bằng JOB Object
        private IntPtr _jobHandle;
        private const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
        const int JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x800;
        const int JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x1000;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr process);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool TerminateJobObject(IntPtr hJob, uint uExitCode);

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_UI_RESTRICTIONS
        {
            public uint UIRestrictionsClass;
        }

        public enum JobObjectInfoType
        {
            JobObjectBasicLimitInformation = 2,
            JobObjectExtendedLimitInformation = 9,
            JobObjectBasicUIRestrictions = 4,
        }

        public const int JOB_OBJECT_LIMIT_CHILD_PROCESS_RESTRICTIONS = 0x00001000;

        public WorkVideoService(IOTContext iOTContext, ILogger<WorkVideoService> logger)
        {
            _iOTContext = iOTContext;
            _logger = logger;
            CameraData = CameraData.getInstance();

            // Create the Job Object
            _jobHandle = CreateJobObject(IntPtr.Zero, "VideoService");
            if (_jobHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Không thể tạo Job Object");
            }

            // Set the job object to terminate all child processes when closed
            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE | JOB_OBJECT_LIMIT_BREAKAWAY_OK
            };

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE | JOB_OBJECT_LIMIT_BREAKAWAY_OK
                }
            };

            var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            var extendedInfoPtr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!SetInformationJobObject(_jobHandle, 9, extendedInfoPtr, (uint)length))
            {
                throw new InvalidOperationException("Không thể thiết lập thông tin Job Object");
            }

            Marshal.FreeHGlobal(extendedInfoPtr);

            // **Cấu hình bổ sung cho Job Object:**
            // Thiết lập hạn chế giao diện người dùng cho Job Object
            var uiRestrictions = new JOBOBJECT_BASIC_UI_RESTRICTIONS
            {
                UIRestrictionsClass = 0 // Bạn có thể thiết lập các cờ hạn chế UI ở đây, ví dụ: 0x00000001 cho JOB_OBJECT_UILIMIT_DESKTOP.
            };

            var uiRestrictionsLength = Marshal.SizeOf(typeof(JOBOBJECT_BASIC_UI_RESTRICTIONS));
            var uiRestrictionsPtr = Marshal.AllocHGlobal(uiRestrictionsLength);
            Marshal.StructureToPtr(uiRestrictions, uiRestrictionsPtr, false);

            if (!SetInformationJobObject(_jobHandle, (int)JobObjectInfoType.JobObjectBasicUIRestrictions, uiRestrictionsPtr, (uint)uiRestrictionsLength))
            {
                throw new InvalidOperationException("Không thể thiết lập UI Restrictions cho Job Object");
            }

            Marshal.FreeHGlobal(uiRestrictionsPtr);
        }

        // Cấu trúc để thiết lập thông tin giới hạn cho Job Object
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public int LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public int ActiveProcessLimit;
            public long Affinity;
            public int PriorityClass;
            public int SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
        public async Task GetVideo(string? timeVideo, string? fileName, string rtspUrl, string contentRoot, string timeOut, CancellationToken stoppingToken)
        {
            string cmdLine = $@"-hwaccel cuda -hwaccel_output_format cuda -t {timeVideo} -rtsp_transport tcp -timeout {timeOut} -i {rtspUrl} -c:v h264_nvenc -an -vf scale_cuda=640:480 -r 25 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel quiet -hide_banner";
            //string cmdLine = $@" -t {timeVideo} -rtsp_transport tcp -timeout {timeOut} -i {rtspUrl} -an -vf scale=640:480 -r 25 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel quiet -hide_banner";

            await RunFFmpegProcess(cmdLine, stoppingToken);

        }


        public async Task<string> CreateConcatTxt(int camId, string ThuMucVideo, DateTime beginDate, DateTime endDate, string tenFileConcatTxt, int timeVideo)
        {
            var txtFileConcat = SeachFile(camId, ThuMucVideo, beginDate, endDate, timeVideo);
            if (!string.IsNullOrEmpty(txtFileConcat))
            {
                string cmdLine = $@"/C ({txtFileConcat}) > {tenFileConcatTxt}";

                await RunCMDProcess(cmdLine);

            }

            return tenFileConcatTxt;
        }

        public async Task ConcatVideo(int camId, string ThuMucDuongDan, string TenFileConcatTxt, DateTime beginDate, DateTime endDate, string contentRoot, string timeOut, int timeVideo)
        {
            var txtFileConcat = await CreateConcatTxt(camId, ThuMucDuongDan, beginDate, endDate, TenFileConcatTxt, timeVideo);
            if (!string.IsNullOrEmpty(txtFileConcat))
            {
                string cmdLine = $@"/C ffmpeg -f concat -safe 0 -i {txtFileConcat} -c copy {contentRoot} -timeout {timeOut}";

                await RunCMDProcess(cmdLine);
            }

        }

        public string SeachFile(int camId, string ThuMucVideo, DateTime beginDate, DateTime endDate, int timeVideo)
        {
            string cmdLine = string.Empty;
            string[]? files = CheckFile(camId, ThuMucVideo, beginDate, endDate, timeVideo);
            if (files?.Length > 0)
            {
                var sortedFiles = files.OrderBy(x => Path.GetFileName(x)).ToArray();

                foreach (var file in sortedFiles)
                {
                    if (file.Length > 0)
                    {
                        cmdLine += $@"echo file '{file}' & ";
                    }
                }
                if (!string.IsNullOrEmpty(cmdLine))
                {
                    cmdLine = cmdLine[..^2];
                }
            }

            return cmdLine;
        }
        public void DeleteFile(string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            bool fileIsOpen = IsFileLocked(file);
            if (!fileIsOpen)
            {
                File.Delete(filePath);
            }
        }

        public string[]? CheckFile(int camId, string ThuMucLay, DateTime beginDate, DateTime endDate, int timeVideo)
        {
            var camIdstring = $@"{camId}_*";
            string cmdLine = string.Empty;

            List<string> listFile = new List<string>();
            String[]? filesReturl = null;
            string[] files = Directory.GetFiles(ThuMucLay, camIdstring, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                using (var stream = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    if (file.Length > 0)
                    {
                        //DateTime creation = File.GetCreationTime(file);
                        string fileName = Path.GetFileName(file);
                        // Lấy tên file không bao gồm phần mở rộng
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        // Tách phần thời gian từ tên file
                        string[] parts = fileNameWithoutExtension.Split('_');
                        if (parts.Length == 2)
                        {
                            string ticksString = parts[1]; // "638707154825520542"

                            // Chuyển đổi từ ticks thành DateTime
                            if (long.TryParse(ticksString, out long ticks))
                            {
                                // DateTime f = new DateTime(ticks);
                                if (beginDate.AddMilliseconds(-timeVideo).Ticks <= ticks 
                                    && ticks < endDate.Ticks)
                                {
                                    listFile.Add(file);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Không thể chuyển đổi ticks thành DateTime.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Tên file không đúng định dạng.");
                        }


                    }
                    stream.Close();
                }

            }

            if (listFile.Count > 0)
            {
                filesReturl = listFile.ToArray();
            }


            return filesReturl;
        }

        public async Task GetImage(string rtspUrl, string contentRoot, string timeOut, bool reSize = true)
        {
            string cmdLine = "";
            if (reSize == true)
            {
                cmdLine = $"/C ffmpeg -rtsp_transport tcp -xerror -timeout {timeOut} -i \"{rtspUrl}\" -vf scale=640:480 -r 25  -maxrate 1M -bufsize 2M -frames:v 1 {contentRoot} -y -loglevel quiet -an -hide_banner & exit /b";
            }
            else
            {
                cmdLine = $"/C ffmpeg -rtsp_transport tcp -xerror -timeout {timeOut} -i \"{rtspUrl}\" -r 25  -maxrate 1M -bufsize 2M -frames:v 1 {contentRoot} -y -loglevel quiet -an -hide_banner & exit /b";
            }

            await RunCMDProcess(cmdLine);

        }
        public async Task GetImageFromVideo(int camId, string ThuMucVideo, DateTime? beginDate, DateTime? endDate, double anhTrenGiay, string contentRoot, int timeVideo)
        {
            string[]? files = null;
            string cmdLine = "";
            if (beginDate.HasValue && endDate.HasValue)
            {
                files = CheckFile(camId, ThuMucVideo, beginDate.Value, endDate.Value, timeVideo);
            }
            else
            {
                files = CheckFile(camId, ThuMucVideo, DateTime.Now.AddSeconds(-5), DateTime.Now, timeVideo);
            }

            if (files?.Length > 0)
            {
                var file = files.First();
                if (file.Length > 0)
                {


                    cmdLine = $@"/C ffmpeg -i {file} -vf fps=1/{anhTrenGiay} {contentRoot} -y -loglevel quiet -an -hide_banner";

                    await RunCMDProcess(cmdLine);
                }
            }

        }
        public async Task CropImage(string sourcePath, string outputPath, int? x, int? y, int? width, int? height)
        {
            string cmdLine = "";

            cmdLine = $@"/C ffmpeg -i {sourcePath} -vf crop={width}:{height}:{x}:{y} {outputPath} -y -loglevel quiet -an -hide_banner";
            await RunCMDProcess(cmdLine);

        }
        public async Task<List<ImageReturn>> FindFile(string path, string nameFile, int? x, int? y, int? width, int? height)
        {
            List<ImageReturn> result = new List<ImageReturn>();
            string[] files = Directory.GetFiles(path, $"{nameFile}*", SearchOption.TopDirectoryOnly);

            List<string> listFile = new List<string>();

            foreach (var file in files)
            {
                using (var stream = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    if (file.Length > 0)
                    {
                        listFile.Add(file);
                    }
                    stream.Close();
                }
            }

            foreach (var file in listFile)
            {
                if (x >= 0 && y >= 0 && width > 0 && height > 0)
                {
                    await CropImage(file, file, x, y, width, height);
                }
                var base64Image = await ImageToBase64(file);

                ImageReturn imageReturn = new ImageReturn();
                imageReturn.Base64 = base64Image;
                imageReturn.ErrMsg = "Lấy ảnh thành công!";

                result.Add(imageReturn);
            }


            return result;
        }


        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            //file is not locked
            return false;
        }

        public async Task<string> ImageToBase64(string path)
        {
            FileInfo file = new FileInfo(path);
            bool fileIsOpen = IsFileLocked(file);
            if (!fileIsOpen)
            {
                byte[] imageArray = await System.IO.File.ReadAllBytesAsync(path);
                string base64String = Convert.ToBase64String(imageArray);
                return base64String;
            }
            return "";
        }

        private Task RunFFmpegProcess(string cmdLine, CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = cmdLine,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Lưu trữ tiến trình vào danh sách
                    CameraData.ffmpegProcesses.Add(process);

                    process.Start();

                    // Gán process vào Job Object
                    AssignProcessToJobObject(_jobHandle, process.Handle);

                    if (!AssignProcessToJobObject(_jobHandle, process.Handle))
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger.LogError($"Failed to assign process to job object. Error code: {error}");
                    }

                    await process.WaitForExitAsync(stoppingToken);

                    // Xóa tiến trình khỏi danh sách sau khi hoàn thành
                    CameraData.ffmpegProcesses.Remove(process);
                }
            }, stoppingToken);
        }

        private Task RunCMDProcess(string cmdLine)
        {
            return Task.Run(async () =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = CMD,
                    Arguments = cmdLine,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Lưu trữ tiến trình vào danh sách
                    CameraData.ffmpegProcesses.Add(process);

                    process.Start();
                   

                    // Gán process vào Job Object
                    AssignProcessToJobObject(_jobHandle, process.Handle);

                    if (!AssignProcessToJobObject(_jobHandle, process.Handle))
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger.LogError($"Failed to assign process to job object. Error code: {error}");
                    }

                    await process.WaitForExitAsync();

                    // Xóa tiến trình khỏi danh sách sau khi hoàn thành
                    CameraData.ffmpegProcesses.Remove(process);
                }
            });
        }

        public void StopProcess()
        {
            // Dừng tất cả các process thuộc Job Object
            if (_jobHandle != IntPtr.Zero)
            {
                TerminateJobObject(_jobHandle, 0);
                _jobHandle = IntPtr.Zero;
            }
        }
    }
}
