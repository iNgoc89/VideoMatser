using FFmpegWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace WorkerVideoCameraService.Services
{
    public class WorkVideoService
    {
        public IOTContext _iOTContext;
        public WorkVideoService(IOTContext iOTContext)
        {
            _iOTContext = iOTContext;
        }
        public void GetVideo(string? fileName, string rtspUrl, string contentRoot)
        {
            string cmdLine = $@"-t 5 -rtsp_transport tcp -i {rtspUrl} -vf scale=640:360 -r 24 -crf 23 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel verbose -an -hide_banner";

            Process process = new();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = cmdLine;
            process.Start();
        }

        public void ConcatVideo(string ThuMucDuongDan, DateTime beginDate, DateTime endDate, string? fileName, string contentRoot)
        {
            var listVideo = SeachFile(ThuMucDuongDan, beginDate, endDate);
            if (!string.IsNullOrEmpty(listVideo))
            {
                string cmdLine = $@" -i ""concat:{listVideo}"" -c copy {contentRoot}";

                Process process = new();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = cmdLine;
                process.Start();
            }
         
        }

   
        public class Video
        {
            public int CameraId { get; set; }
            public DateTime Time { get; set; }
        }
        public void Refresh(TimeSpan timeSpan, string? fileName)
        {
            if (timeSpan.TotalMinutes == 30)
            {
                Process process = new();
                process.StartInfo.FileName = fileName;
                process.Refresh();
            }
        }

        public string SeachFile(string ThuMucDuongDan, DateTime beginDate, DateTime endDate)
        {
            string textList = string.Empty;
         
            string[] files = Directory.GetFiles(ThuMucDuongDan);
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    
                    var cutright = file[..^4];
                    var cutleft = cutright.Substring(cutright.LastIndexOf('_'), cutright.Length - cutright.LastIndexOf('_'));

                    var catleft2 = cutleft[1..];

                    DateTime? dateTime;
                    if (catleft2.Length > 0)
                    {
                        long datetimeFile;
                        long.TryParse(catleft2, out datetimeFile);
                        if (datetimeFile > 0)
                        {
                            dateTime = new DateTime(datetimeFile);

                            if (dateTime >= beginDate && dateTime <= endDate)
                            {
                                textList += file + "|";
                            }
                        }
                        

                    }

                }

            }
            return textList;
        }
        public void DeleteFile(string filePath)
        {
            System.IO.File.Delete(filePath);
        }
        public void TestCMD(string contentRoot)
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
