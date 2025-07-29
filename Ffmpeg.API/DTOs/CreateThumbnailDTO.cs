namespace FFmpeg.API.DTOs
{
    public class CreateThumbnailDTO
    {
        public IFormFile VideoName { get; set; }
        public string ImageName { get; set; }
        public string Time { get; set; } = "00:00:00";
    }
}
