namespace FFmpeg.API.DTOs
{
    public class VideoSpeedChangeDto
    {
        public IFormFile VideoFile { get; set; }

        public double Speed { get; set; }

        public string OutputFileName { get; set; }
    }
}

