using Ffmpeg.Command.Commands;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Infrastructure.Commands
{
    public class CropCommand : BaseCommand, ICommand<CropModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public CropCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder) : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(CropModel model)
        {
            if (string.IsNullOrEmpty(model.StartTime) || string.IsNullOrEmpty(model.EndTime))
            {
                throw new ArgumentException("StartTime and EndTime are required");
            }

            var durationFilter = $"-ss {model.StartTime} -to {model.EndTime}";

            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .AddOption(durationFilter)
                .AddOption("-c copy") // copy streams without re-encoding
                .SetOutput(model.OutputFile);

            return await RunAsync();
        }
    }
}
