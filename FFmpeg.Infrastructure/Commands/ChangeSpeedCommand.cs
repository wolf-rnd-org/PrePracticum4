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
            double atempoValue = model.Speed;

            // FFmpeg תומכת בטווח atempo רק בין 0.5 ל-2.0
            // אז אם המהירות מחוץ לטווח הזה – נפרק אותה לרמות חוקיות
            List<string> atempoFilters = new();
            while (atempoValue > 2.0)
            {
                atempoFilters.Add("atempo=2.0");
                atempoValue /= 2.0;
            }
            while (atempoValue < 0.5)
            {
                atempoFilters.Add("atempo=0.5");
                atempoValue *= 2.0;
            }
            atempoFilters.Add($"atempo={atempoValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            string atempoFinal = string.Join(",", atempoFilters);

            CommandBuilder = _commandBuilder
                .SetInput(model.InputFile)
                .AddOption($"-filter_complex \"[0:v]setpts={setptsValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}*PTS[v];[0:a]{atempoFinal}[a]\"")
                .AddOption("-map \"[v]\"")
                .AddOption("-map \"[a]\"");

            if (model.IsVideo)
            {
                CommandBuilder.SetVideoCodec(model.VideoCodec);
            }

            CommandBuilder.SetOutput(model.OutputFile, model.IsVideo ? false : true);

            return await RunAsync();
        }
    }
}
