﻿namespace FFmpeg.API.DTOs
{
    public class VideoCompreesionDto
    {
        public IFormFile VideoFile { get; set; }
        public string OutputFileName { get; set; }
    }
}
