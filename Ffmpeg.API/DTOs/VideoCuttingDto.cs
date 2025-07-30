namespace FFmpeg.API.DTOs
{
    public class VideoCuttingDto
    {
        public required IFormFile VideoFile { get; set; }
        public required string StartTime { get; set; } // Format: HH:MM:SS or SS
        public required string EndTime { get; set; }   // Format: HH:MM:SS or SS
        public string? OutputName { get; set; } // Optional custom output name
    }
}
