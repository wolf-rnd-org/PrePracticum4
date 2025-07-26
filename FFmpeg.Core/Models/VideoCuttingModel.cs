using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class VideoCuttingModel
    {
        public required string InputFile { get; set; }
        public required string OutputFile { get; set; }
        public required string StartTime { get; set; } // Format: HH:MM:SS or SS
        public required string EndTime { get; set; }   // Format: HH:MM:SS or SS
    }
}
