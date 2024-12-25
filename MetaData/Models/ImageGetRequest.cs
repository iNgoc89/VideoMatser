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

        public bool Resize { get; set; } 
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
