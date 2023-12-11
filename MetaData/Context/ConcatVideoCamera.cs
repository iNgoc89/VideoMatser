#nullable disable
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaData.Context
{
    [Table("ConcatVideoCamera")]
    public class ConcatVideoCamera
    {
        public int Id { get; set; }
        public Guid GID { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public string VideoUri { get; set; }
        public int Status { get; set; }
        public int CameraId { get; set; }
    }
}
