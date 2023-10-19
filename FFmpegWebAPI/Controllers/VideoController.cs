﻿using FFmpegWebAPI.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FFmpegWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        public readonly IOTContext _iOTContext;
        public VideoController(IOTContext iOTContext) 
        {
            _iOTContext = iOTContext;
            _iOTContext = iOTContext;
            _iOTContext = iOTContext;
            _iOTContext = iOTContext;
            _iOTContext = iOTContext;
        }
        // GET: api/<VideoController>
        [HttpGet]
        public IEnumerable<Camera> Get()
        {
            var data = _iOTContext.Cameras?.ToList();
            return data;
        }

        // GET api/<VideoController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<VideoController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<VideoController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<VideoController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
