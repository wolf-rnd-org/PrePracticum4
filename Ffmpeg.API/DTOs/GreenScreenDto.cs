namespace FFmpeg.API.DTOs
{
    //public class GreenScreenDto
    //{
    //    public IFormFile VideoFile { get; set; }
    //    public IFormFile BackgroundFile { get; set; }
    //    public string ColorToReplace { get; set; } = "green";
    //    public int Similarity { get; set; } = 0;
    //}
    public class GreenScreenDto
    {
        public IFormFile VideoFile { get; set; }
        public string ColorToRemove { get; set; } = "green"; // ברירת מחדל לירוק
        public string BackgroundFile { get; set; } // אופציונלי אם רוצים להוסיף רקע חדש
    }

}
