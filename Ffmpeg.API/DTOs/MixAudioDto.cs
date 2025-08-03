using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FFmpeg.API.DTOs
{
    public class MixAudioDto
    {
        [FromForm]
        public IFormFile AudioFile1 { get; set; }

        [FromForm]
        public IFormFile AudioFile2 { get; set; }
    }
}

