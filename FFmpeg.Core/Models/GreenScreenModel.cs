using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class GreenScreenModel
    {
        public string InputFile { get; set; }          // קובץ עם מסך ירוק
        public string BackgroundFile { get; set; }     // קובץ הרקע החדש
        public string OutputFile { get; set; }         // קובץ הווידאו הסופי
        public string ChromaColor { get; set; } = "0x00FF00"; // ברירת מחדל ירוק
        public double Similarity { get; set; } = 0.1;
        public double Blend { get; set; } = 0.2;
        public string VideoCodec { get; set; } = "libx264"; // קודק ברירת מחדל
    }
}
