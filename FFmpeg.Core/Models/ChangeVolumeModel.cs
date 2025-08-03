using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class ChangeVolumeModel
    {
        public string InputFile { get; set; }  // קובץ וידאו/אודיו
        public string OutputFile { get; set; } // קובץ פלט
        public double VolumeLevel { get; set; } = 1.0; // 1.0 = עוצמה רגילה, 2.0 = כפול, 0.5 = חצי
        public bool IsVideo { get; set; } = true;
        public string VideoCodec { get; set; } = "libx264";
    }
}
