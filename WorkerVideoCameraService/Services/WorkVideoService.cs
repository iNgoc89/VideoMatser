using FFmpegWebAPI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace WorkerVideoCameraService.Services
{
    public class WorkVideoService : IDisposable
    {
        public IOTContext _iOTContext;
        public string txtCmdConcat = string.Empty;
        // trường lưu trạng thái Dispose
        private bool m_Disposed = false;

        private StreamWriter stream;


        // Phương thức triển khai từ giao diện
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public WorkVideoService(IOTContext iOTContext, string filename)
        {
            _iOTContext = iOTContext;
            stream = new StreamWriter(filename, true);
        }
        public void GetVideo(string? fileName, string rtspUrl, string contentRoot)
        {
            string cmdLine = $@"-t 5 -rtsp_transport tcp -i {rtspUrl} -vf scale=640:360 -r 24 -crf 23 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel verbose -an -hide_banner";

            Process process = new();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = cmdLine;
            process.Start();
        }


        public string CreateConcatTxt(int camId, string ThuMucVideo, DateTime beginDate, DateTime endDate, string tenFileConcatTxt)
        {
            var txtFileConcat = SeachFile(camId, ThuMucVideo, beginDate, endDate);
            if (!string.IsNullOrEmpty(txtFileConcat))
            {
                string cmdLine = $@"/C ({txtFileConcat}) > {tenFileConcatTxt}";

                Process process = new();
                process.StartInfo.FileName = "CMD.exe";
                process.StartInfo.Arguments = cmdLine;
                process.Start();

            }

            return tenFileConcatTxt;
        }

        public void ConcatVideo(int camId, string ThuMucDuongDan, string TenFileConcatTxt, DateTime beginDate, DateTime endDate, string? fileName, string contentRoot)
        {
            var txtFileConcat = CreateConcatTxt(camId, ThuMucDuongDan, beginDate, endDate, TenFileConcatTxt);
            if (!string.IsNullOrEmpty(txtFileConcat))
            {
                string cmdLine = $@" -f concat -safe 0 -i {txtFileConcat} -c copy {contentRoot}";

                Process process = new();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = cmdLine;
                process.Start();
            }
         
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
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
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
                                    listFile.Add(file);

                                }
                            }
                        }
                    }
                    stream.Close();
                    stream.Dispose();
                }
            
            }

            if (listFile.Count > 0)
            {
                filesReturl = listFile.ToArray();
            }

            return filesReturl;
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
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    component.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                CloseHandle(handle);
                handle = IntPtr.Zero;

                // Note disposing has been done.
                disposed = true;
            }
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
