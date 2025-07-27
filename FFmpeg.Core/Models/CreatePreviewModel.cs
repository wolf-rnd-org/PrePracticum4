using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Models
{
    public class CreatePreviewModel
    {
        public string VideoNmae { get; set; }
        public string OutputImageName { get; set; }
        public string Time { get; set; } = "00:00:00";

    }
}
