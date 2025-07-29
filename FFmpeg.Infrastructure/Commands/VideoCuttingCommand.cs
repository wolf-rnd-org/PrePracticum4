using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Commands;
using FFmpeg.Infrastructure.Services;
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
    public class VideoCuttingCommand : BaseCommand, ICommand<VideoCuttingModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public VideoCuttingCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(VideoCuttingModel model)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(model.InputFile))
                throw new ArgumentException("Input file is required", nameof(model.InputFile));
            
            if (string.IsNullOrEmpty(model.OutputFile))
                throw new ArgumentException("Output file is required", nameof(model.OutputFile));
            
            if (string.IsNullOrEmpty(model.StartTime))
                throw new ArgumentException("Start time is required", nameof(model.StartTime));
            
            if (string.IsNullOrEmpty(model.EndTime))
                throw new ArgumentException("End time is required", nameof(model.EndTime));

            // Build the command: ffmpeg -i input.mp4 -ss 00:00:05 -to 00:00:10 -c copy output.mp4
            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .SetStartTime(model.StartTime)
                .SetEndTime(model.EndTime)
                .SetCopyCodec()
                .SetOutput(model.OutputFile);

            return await RunAsync();
        }
    }
}
