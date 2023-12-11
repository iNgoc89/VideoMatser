using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaData.Models
{
    public class ImageReturn
    {
        public string? Base64 { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;
        public string? ErrMsg { get; set; } = string.Empty;
    }
}
