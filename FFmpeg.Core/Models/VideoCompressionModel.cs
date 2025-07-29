using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class VideoCompressionModel
    {
        public required string InputFile { get; set; }
        public required string OutputFile { get; set; }
    }
}
