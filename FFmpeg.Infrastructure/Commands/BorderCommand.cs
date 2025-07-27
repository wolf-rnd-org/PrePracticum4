
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ffmpeg.Command.Commands;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Commands;
using global::FFmpeg.Core.Models;
using global::FFmpeg.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace FFmpeg.Infrastructure.Commands
{
    public class BorderCommand : BaseCommand, ICommand<BorderModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public BorderCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(BorderModel model)
        {
            string padFilter = $"pad=width=iw+{model.BorderThickness * 2}:height=ih+{model.BorderThickness * 2}:x={model.BorderThickness}:y={model.BorderThickness}:color={model.BorderColor}";

            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .AddFilterComplex(padFilter) // Replaced AddFilter with AddFilterComplex
                .SetVideoCodec(model.VideoCodec)
                .SetOutput(model.OutputFile, false);

            return await RunAsync();
        }
    }
}

