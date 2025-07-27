using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class BorderModel
    {
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public string BorderColor { get; set; } = "black";
        public int BorderThickness { get; set; } = 20; 
        public string VideoCodec { get; set; } = "libx264";
    }
}
