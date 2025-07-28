namespace FFmpeg.API.DTOs
{
    public  VideoSpeedChangeDto
    {
            public IFormFile VideoFile { get; set; }

            public double Speed{ get; set; } 

            public string OutputFileName { get; set; }
    }
}
