namespace FFmpeg.API.DTOs
{
    public class BorderDto
    {
        public IFormFile VideoFile { get; set; }
        public string BorderColor { get; set; } = "black";
        public string OutputFileName { get; set; }
    }
}
