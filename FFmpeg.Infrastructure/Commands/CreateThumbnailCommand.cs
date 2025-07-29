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
    public class CreateThumbnailCommand : BaseCommand, ICommand<CreateThumbnailModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public CreateThumbnailCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(CreateThumbnailModel model)
        {
            if (string.IsNullOrEmpty(model.VideoName) || string.IsNullOrEmpty(model.ImageName))
                throw new ArgumentException("Video name and output image name are required.");

            CommandBuilder = _commandBuilder
                .SetInput(model.VideoName)
                .AddOption($"-ss {model.Timestamp}")
                .AddOption("-vframes 1")
                .SetOutput(model.ImageName, true);

            return await RunAsync();
        }
    }
}
