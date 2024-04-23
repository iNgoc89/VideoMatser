using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Models
{
    public class ImageFromVideoRequest
    {
        public Guid GID { get; set; }
        public int CameraId { get; set; }

        public double AnhTrenGiay { get; set; }
        public DateTime? BeginDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
