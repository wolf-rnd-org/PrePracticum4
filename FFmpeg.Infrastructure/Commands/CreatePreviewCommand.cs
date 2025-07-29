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
    public class CreatePreviewCommand : BaseCommand, ICommand<CreatePreviewModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public CreatePreviewCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(CreatePreviewModel model)
        {
            if (string.IsNullOrEmpty(model.VideoNmae) || string.IsNullOrEmpty(model.OutputImageName))
                throw new ArgumentException("Video name and output image name are required.");

            CommandBuilder = _commandBuilder
                .SetInput(model.VideoNmae)
                .AddOption($"-ss {model.Time}")
                .AddOption("-vframes 1")
                .SetOutput(model.OutputImageName, true);

            return await RunAsync();
        }
    }
}
