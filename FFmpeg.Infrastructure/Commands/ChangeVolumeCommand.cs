using Ffmpeg.Command.Commands;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Infrastructure.Commands
{
    public class ChangeVolumeCommand : BaseCommand, ICommand<ChangeVolumeModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public ChangeVolumeCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(ChangeVolumeModel model)
        {
            if (model.VolumeLevel < 0)
                throw new ArgumentException("Volume must be zero or a positive number.");

            string volumeFilter = $"volume={model.VolumeLevel.ToString(CultureInfo.InvariantCulture)}";

            //הגדרת הפקודה
            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .AddOption($"-af \"{volumeFilter}\"");

            if (model.IsVideo)
            {
                CommandBuilder.SetVideoCodec(model.VideoCodec);
            }

            CommandBuilder.SetOutput(model.OutputFile, model.IsVideo ? false : true);//הגדרת קובץ הפלט

            return await RunAsync();
        }
    }
}
