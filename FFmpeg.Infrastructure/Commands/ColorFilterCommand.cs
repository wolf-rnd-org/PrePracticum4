﻿using Ffmpeg.Command.Commands;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Infrastructure.Commands
{
    public class ColorFilterCommand : BaseCommand, ICommand<ColorFilterModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public ColorFilterCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(ColorFilterModel model)
        {
            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .AddOption("-vf \"hue=s=0\"");

            if (model.IsVideo)
            {
                CommandBuilder.SetVideoCodec(model.VideoCodec);
            }

            CommandBuilder.SetOutput(model.OutputFile, !model.IsVideo);

            return await RunAsync();
        }
    }
}
