using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FFmpegWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        public readonly IOTContext _iOTContext;
        public IOTService _iOTService;
        public VideoController(IOTContext iOTContext, IOTService iOTService) 
        {
            _iOTContext = iOTContext;
            _iOTService = iOTService;
        }

        // GET api/<VideoController>/5
        [HttpGet("{GID}")]
        public ConcatVideoCamera? Get(Guid GID)
        {
            var data = _iOTContext?.ConcatVideoCameras?.Where(x=>x.GID == GID)?.FirstOrDefault();
            return data;
        }

        // POST api/<VideoController>
        [HttpPost]
        public int Post(Guid GID, int cameraId, string beginDate, string endDate)
        {
            int kq = 0;
            DateTime.TryParse(beginDate, out DateTime BD);
            DateTime.TryParse(endDate, out DateTime ED);
            if (BD > DateTime.MinValue && ED > DateTime.MinValue)
            {
                 kq = _iOTService.P_ConcatVideoCamera_Insert(GID, cameraId, BD, ED);
            }
          
            return kq;
        }

    
    }
}
