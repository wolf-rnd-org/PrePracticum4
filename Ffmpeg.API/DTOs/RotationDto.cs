namespace FFmpeg.API.DTOs
{
    public class RotationDto
    {
        public IFormFile InputFile { get; set; }
        public int Angle { get; set; }
        //public string OutputFile { get; set; }
    }
}
