using FFmpegWebAPI.Models;
using System.Diagnostics;

namespace FFmpegWebAPI.Services
{
    public class WorkVideoService
    {
        public IOTContext _iOTContext;
        public string txtCmdConcat = string.Empty;

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
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                }
                else Debug.WriteLine("File not found");
            }
            catch (IOException ioExp)
            {
                Debug.WriteLine(ioExp.Message);
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
                        var cutleft = cutright[cutright.LastIndexOf('_')..];

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
    }
}
