using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Context
{
#nullable disable
    [Table("CameraBusiness")]
    public class CameraBusiness
    {
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public int CameraId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public Camera Camera { get; set;}
    }
}
