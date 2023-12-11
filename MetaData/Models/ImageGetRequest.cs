using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Models
{
    public class ImageGetRequest
    {
        public Guid GID { get; set; }
        public int CameraId { get; set; }
        public bool SaveImage { get; set; }
    }
}
