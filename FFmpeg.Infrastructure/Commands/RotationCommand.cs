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
    public class RotationCommand : BaseCommand, ICommand<RotationModel>
    {
        private readonly ICommandBuilder _commandBuilder;
        public RotationCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder) : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(RotationModel rotationModel)
        {
            CommandBuilder = _commandBuilder
                 .SetInput(rotationModel.InputFile)
                 .AddOption($"-vf \"rotate={rotationModel.Angle}*PI/180\"")
                 .AddOption($"-map 0:v?")
                 .AddOption($"-map 0:a?")
                 .AddOption($"-c:a copy");

            CommandBuilder.SetOutput(rotationModel.OutputFile, false);

            return await RunAsync();

        }
    }
}
