namespace FFmpegWebAPI.Data
{
    public class VideoReturl
    {
        public int Id { get; set; }
        public Guid GID { get; set; }
        public string? UrlPath { get; set; } = string.Empty;

        public string? ErrMsg { get; set; } = string.Empty;
    }
}
