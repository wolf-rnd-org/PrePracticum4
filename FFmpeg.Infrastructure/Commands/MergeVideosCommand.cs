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
    public class MergeVideosCommand : BaseCommand, ICommand<MergeVideosModel>
    {
        private readonly ICommandBuilder _commandBuilder;

        public MergeVideosCommand(FFmpegExecutor executor, ICommandBuilder commandBuilder)
            : base(executor)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
        }

        public async Task<CommandResult> ExecuteAsync(MergeVideosModel model)
        {
            string filterComplex;
            if (model.Direction == MergeDirection.Horizontal)
            {
                filterComplex = "[0:v][1:v]hstack=inputs=2";
            }
            else 
            {
                filterComplex = "[0:v][1:v]vstack=inputs=2";
            }

            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile1)
                .SetInput(model.InputFile2)
                .AddFilterComplex(filterComplex)
                .SetOutput(model.OutputFile);
            
            return await RunAsync();
        }
    }
}
