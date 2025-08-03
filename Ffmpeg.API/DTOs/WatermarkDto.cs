namespace FFmpeg.API.DTOs
{
    public class WatermarkDto
    {
        public required IFormFile VideoFile { get; set; }
        public required IFormFile WatermarkFile { get; set; }
        public int XPosition { get; set; } = 10;
        public int YPosition { get; set; } = 10;
    }
}
