using Ffmpeg.Command.Commands;
using FFmpeg.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.Core.Models;

namespace FFmpeg.Infrastructure.Commands
{
    public class ChangeSpeedCommand : BaseCommand, ICommand<ChangeSpeedModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public ChangeSpeedCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(ChangeSpeedModel model)
        {
            if (model.Speed <= 0)
                throw new ArgumentException("Speed must be a positive number.");

            double setptsValue = 1.0 / model.Speed;

            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .AddOption($"-vf setpts={setptsValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}*PTS")
                .AddOption($"-map 0:a?")
                .AddOption($"-c:a copy");

            if (model.IsVideo)
            {
                CommandBuilder.SetVideoCodec(model.VideoCodec); 
            }

            CommandBuilder.SetOutput(model.OutputFile, model.IsVideo ? false : true);

            return await RunAsync();
        }
    }
}
