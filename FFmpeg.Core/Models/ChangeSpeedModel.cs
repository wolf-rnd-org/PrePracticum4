﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class ChangeSpeedModel
    {
        public string InputFile { get; set; } 

        public string OutputFile { get; set; }

        public double Speed { get; set; } = 1.0; 

        public bool IsVideo { get; set; } = true;

        public string VideoCodec { get; set; } = "libx264";
    }
}
