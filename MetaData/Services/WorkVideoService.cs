using MetaData.Context;
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

namespace MetaData.Services
{
    public class WorkVideoService
    {
        public IOTContext _iOTContext;
        public string txtCmdConcat = string.Empty;
        public string CMD = "CMD.exe";
        private readonly ILogger<WorkVideoService> _logger;
        public WorkVideoService(IOTContext iOTContext, ILogger<WorkVideoService> logger)
        {
            _iOTContext = iOTContext;
            _logger = logger;
        }
        public async Task GetVideo(string? timeVideo, string? fileName, string rtspUrl, string contentRoot, string timeOut, CancellationToken stoppingToken)
        {
            string cmdLine = $@"-hwaccel cuda -hwaccel_output_format cuda -t {timeVideo} -rtsp_transport tcp -timeout {timeOut} -i {rtspUrl} -c:v h264_nvenc -an -vf scale_cuda=640:480 -r 25 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel quiet -hide_banner";
           //string cmdLine = $@" -t {timeVideo} -rtsp_transport tcp -timeout {timeOut} -i {rtspUrl} -an -vf scale=640:480 -r 25 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel quiet -hide_banner";

            await RunFFmpegProcess(cmdLine, contentRoot, stoppingToken);
        }


        public async Task<string> CreateConcatTxt(int camId, string ThuMucVideo, DateTime beginDate, DateTime endDate, string tenFileConcatTxt)
        {
            var txtFileConcat = SeachFile(camId, ThuMucVideo, beginDate, endDate);
            if (!string.IsNullOrEmpty(txtFileConcat))
            {
                string cmdLine = $@"/C ({txtFileConcat}) > {tenFileConcatTxt}";

                await RunProcessAsync(CMD, cmdLine);

            }

            return tenFileConcatTxt;
        }

        public async Task ConcatVideo(int camId, string ThuMucDuongDan, string TenFileConcatTxt, DateTime beginDate, DateTime endDate, string contentRoot, string timeOut)
        {
            var txtFileConcat = await CreateConcatTxt(camId, ThuMucDuongDan, beginDate, endDate, TenFileConcatTxt);
            if (!string.IsNullOrEmpty(txtFileConcat))
            {
                string cmdLine = $@"/C ffmpeg -f concat -safe 0 -i {txtFileConcat} -c copy {contentRoot} -timeout {timeOut}";

                await RunProcessAsync(CMD, cmdLine);
            }

        }

        public string SeachFile(int camId, string ThuMucVideo, DateTime beginDate, DateTime endDate)
        {
            string cmdLine = string.Empty;
            string[]? files = CheckFile(camId, ThuMucVideo, beginDate, endDate);
            if (files?.Length > 0)
            {
                foreach (var file in files)
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

        public string[]? CheckFile(int camId, string ThuMucLay, DateTime beginDate, DateTime endDate)
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
                        DateTime creation = File.GetCreationTime(file);

                        if (creation >= beginDate && creation <= endDate)
                        {
                            listFile.Add(file);
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

        public async Task GetImage(string rtspUrl, string contentRoot, string timeOut)
        {
            string cmdLine = "";

            cmdLine = $@"/C ffmpeg -rtsp_transport tcp -xerror -timeout {timeOut} -i {rtspUrl} -vf scale=640:480 -r 25  -maxrate 1M -bufsize 2M -frames:v 1 {contentRoot} -y -loglevel quiet -an -hide_banner & exit /b";

            await RunProcessAsync(CMD, cmdLine);

        }
        public async Task GetImageFromVideo(int camId, string ThuMucVideo, DateTime? beginDate, DateTime? endDate, double anhTrenGiay, string contentRoot)
        {
            string[]? files = null;
            string cmdLine = "";
            if (beginDate.HasValue && endDate.HasValue)
            {
                files = CheckFile(camId, ThuMucVideo, beginDate.Value, endDate.Value);
            }
            else
            {
                files = CheckFile(camId, ThuMucVideo, DateTime.Now.AddSeconds(-5), DateTime.Now);
            }

            if (files?.Length > 0)
            {
                var file = files.First();
                if (file.Length > 0)
                {


                    cmdLine = $@"/C ffmpeg -i {file} -vf fps=1/{anhTrenGiay} {contentRoot} -y -loglevel quiet -an -hide_banner";

                    await RunProcessAsync(CMD, cmdLine);
                }
            }

        }
        public async Task CropImage(string sourcePath, string outputPath, int? x, int? y, int? width, int? height)
        {
            string cmdLine = "";

            cmdLine = $@"/C ffmpeg -i {sourcePath} -vf crop={width}:{height}:{x}:{y} {outputPath} -y -loglevel quiet -an -hide_banner";
            await RunProcessAsync(CMD, cmdLine);

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
                   await CropImage(file, file,x, y, width, height); 
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
        public static Task<int> RunProcessAsync(string? fileName, string argument)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    Arguments = argument,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                },

                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }

        private Task RunFFmpegProcess(string cmdLine, string contentRoot, CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = cmdLine,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            _logger.LogInformation($"[Camera {contentRoot}] {e.Data}");
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            //if (e.Data.Contains("Connection timed out") || e.Data.Contains("Network is unreachable") || e.Data.Contains("Immediate exit requested") || e.Data.Contains("Starting connection attempt"))
                            //{
                            //    _logger.LogWarning($"[Camera {contentRoot}] Network error detected, attempting to reconnect...");
                            //}
                            _logger.LogError($"[Camera {contentRoot}] ERROR: {e.Data}");
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    _logger.LogInformation($"FFmpeg process for camera {contentRoot} exited with code {process.ExitCode}");

                }
            }, stoppingToken);
        }
    }
}
