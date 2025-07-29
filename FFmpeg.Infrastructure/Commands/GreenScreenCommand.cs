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
                    // יצירת הפילטר לפי הנתונים שקיבלנו
                    //string filterExpression = "[0:v]chromakey=0x00FF00:0.1:0.2[ckout];[1:v][ckout]overlay[out]";
                    string filterExpression = $"[0:v]chromakey={model.ChromaColor}:{model.Similarity}:{model.Blend}[ckout];[1:v][ckout]overlay[out]";

                    CommandBuilder = _commandBuilder
                        .SetInput(model.InputFile)
                        .SetInput(model.BackgroundFile)
                        .AddFilterComplex(filterExpression)
                        .SetVideoCodec(model.VideoCodec)
                        //.AddOption("-map [out]")
                        .AddOption("-map 0:a?") // שמירת אודיו אם יש
                        .AddOption("-c:a copy")
                        .SetOutput(model.OutputFile);

                    return await RunAsync();
                }
            }
        }
