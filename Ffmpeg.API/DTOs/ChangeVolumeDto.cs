namespace FFmpeg.API.DTOs
{
    public class ChangeVolumeDto
    {
        public IFormFile AudioFile { get; set; }
        public double VolumeLevel { get; set; }
        public string OutputFileName { get; set; }

    }
}
