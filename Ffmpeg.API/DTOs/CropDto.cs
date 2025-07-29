namespace FFmpeg.API.DTOs
{
    public class CropDto
    {
        public IFormFile VideoFile { get; set; }
        public string StartTime { get; set; } // format HH:mm:ss
        public string EndTime { get; set; }   // format HH:mm:ss
        public string OutputFileName { get; set; }
    }
}
