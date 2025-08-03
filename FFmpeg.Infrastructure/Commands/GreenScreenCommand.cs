using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Commands;
using FFmpeg.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ffmpeg.Command.Commands
{
    public class GreenScreenCommand : BaseCommand, ICommand<GreenScreenModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public GreenScreenCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(GreenScreenModel model)
        {
            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .SetInput(model.BackgroundFile)
                .AddOption($"-filter_complex \"[0:v]chromakey={model.ChromaColor}:{model.Similarity}:{model.Blend}[ckout];[1:v][ckout]overlay[out]\"")
                .AddOption("-map \"[out]\"")
                .SetVideoCodec(model.VideoCodec);
            CommandBuilder.SetOutput(model.OutputFile, false);

            return await RunAsync();
        }
    }
}
