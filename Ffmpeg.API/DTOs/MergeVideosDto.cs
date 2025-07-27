using FFmpeg.Core.Models;

namespace FFmpeg.API.DTOs
{
    public class MergeVideosDto
    {
        public IFormFile VideoFile1 { get; set; } 
        public IFormFile VideoFile2 { get; set; } 
        public MergeDirection Direction { get; set; } = MergeDirection.Horizontal; 
    }
}
