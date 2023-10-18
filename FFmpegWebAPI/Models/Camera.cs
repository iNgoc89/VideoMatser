using System.ComponentModel.DataAnnotations;

namespace FFmpegWebAPI.Models
{
    public class Camera
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        [StringLength(100)] 
        
        public string Name { get; set; }
        [Required]
        [StringLength(100)]
        public string Code { get; set; }
        [Required]
        public int Index { get; set; }
        [StringLength(1000)]
        public string Description { get; set; }
        [Required]
        [StringLength(50)]
        public string IpAddress { get; set; }
        [Required]
        public int Port { get; set; }
        [Required]
        [StringLength(500)]
        public string RtspUrl { get; set; }
        [Required]
        [StringLength(50)]
        public string UserName { get; set; }
        [Required]
        [StringLength(50)]
        public string Password { get; set; }
        [Required]
        public int EndpointStatus { get; set; }
        [Required]
        public int Type { get; set; }
        [Required]
        public bool IsActive { get; set; }

    }
}
