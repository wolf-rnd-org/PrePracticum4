﻿using Ffmpeg.Command.Commands;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Infrastructure.Commands
{
    public class VideoCompressionCommand : BaseCommand, ICommand<VideoCompreesinModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public VideoCompressionCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }


        public async Task<CommandResult> ExecuteAsync(VideoCompreesinModel model)

        {
            CommandBuilder = _commandBuilder

            .SetInput(model.InputFile)
            .SetVideoCodec("libx264")
            .AddOption("-crf 28") // איכות נמוכה יותר = קובץ קטן יותר
            .SetOutput(model.OutputFile);


            return await RunAsync();
        }
    }
}
