﻿using FFmpeg.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Core.Interfaces
{
    public interface ICommandRunner
    {
        Task<Result> RunAsync();
    }
}
