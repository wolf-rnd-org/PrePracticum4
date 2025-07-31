
namespace FFmpeg.API.DTOs
{
    public class VideoSpeedChangeDto
    {
        public required IFormFile VideoFile { get; set; }
        public required double Speed { get; set; }  // לדוגמה 0.5 להאטה או 2 להאצה
        public string? OutputFileName { get; set; }
    }
}
