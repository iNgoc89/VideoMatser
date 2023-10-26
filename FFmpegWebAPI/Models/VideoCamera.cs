namespace FFmpegWebAPI.Models
{
    public partial class VideoCamera
    {
        public int Id { get; set; }

        public int CameraId { get; set; }

        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }

        public string VideoUri { get; set; } = string.Empty;

        public byte Status { get; set; }
        public virtual Camera? Camera { get; set; }


    }
}
