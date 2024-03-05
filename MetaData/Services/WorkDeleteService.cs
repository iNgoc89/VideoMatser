using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Services
{
    public class WorkDeleteService
    {
        public XmhtService _xmhtService;
        public WorkVideoService _workVideo;
        public WorkDeleteService(XmhtService xmhtService, WorkVideoService workVideo) 
        {
            _xmhtService = xmhtService;
            _workVideo = workVideo;
        }

        public async Task DeleteFiles(long? thuMucLay, double timeDelete, CancellationToken stoppingToken)
        {
            var kq = _xmhtService.P_ThuMuc_LayTheoID(null, thuMucLay);
            if (kq != null)
            {
                string? ThuMucDuongDan = kq.DuongDan;

                while (!stoppingToken.IsCancellationRequested)
                {
                    string[] files = Directory.GetFiles(ThuMucDuongDan, "*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        if (file.Length > 0)
                        {
                            DateTime creation = File.GetCreationTime(file);

                            if (creation < DateTime.Now.AddMinutes(-timeDelete))
                            {
                                _workVideo.DeleteFile(file);
                            }
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
