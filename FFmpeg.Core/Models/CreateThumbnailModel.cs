using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class CreateThumbnailModel
    {
        public string VideoName { get; set; }
        public string ImageName { get; set; }
        public TimeSpan Timestamp { get; set; }
    }
}
