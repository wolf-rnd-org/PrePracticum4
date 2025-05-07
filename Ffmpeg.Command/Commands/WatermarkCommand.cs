using Ffmpeg.Command.Requests;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ffmpeg.Command.Commands
{
    public class WatermarkCommand : BaseCommand, ICommand<WatermarkRequest>
    {
        private readonly ICommandBuilder _commandBuilder;

        public WatermarkCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<Result> ExecuteAsync(WatermarkRequest request)
        {
            CommandBuilder = _commandBuilder
                .SetInput(request.InputFile)
                .SetInput(request.WatermarkFile)
                .SetOverlay(request.XPosition, request.YPosition)
                .AddOption($"-map 0:a?")
                .AddOption($"-c:a copy");

            if (request.IsVideo)
            {
                CommandBuilder
                    .SetVideoCodec(request.VideoCodec);
            }

            CommandBuilder.SetOutput(request.OutputFile, request.IsVideo ? false : true);

            return await RunAsync();
        }
    }
}
