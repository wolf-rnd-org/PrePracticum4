using Ffmpeg.Command.Commands;
using Ffmpeg.Command.Requests;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ffmpeg.Command
{
    public interface IFFmpegServiceFactory
    {
        ICommand<WatermarkRequest> CreateWatermarkCommand();
        //ICommand<TrimVideoRequest> CreateTrimVideoCommand();
        //ICommand<ConvertVideoRequest> CreateConvertVideoCommand();
        //ICommand<ExtractFrameRequest> CreateExtractFrameCommand();
    }

    public class FFmpegServiceFactory : IFFmpegServiceFactory
    {
        private readonly FFmpegExecutor _executor;
        private readonly ICommandBuilder _commandBuilder;

        public FFmpegServiceFactory(IConfiguration configuration, ILogger logger = null)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = Path.Combine(baseDirectory, "ffmpeg", "ffmpeg.exe");

            //string ffmpegPath = configuration["FFmpeg:ExecutablePath"];
            bool logOutput = bool.TryParse(configuration["FFmpeg:LogOutput"], out bool log) && log;

            _executor = new FFmpegExecutor(ffmpegPath, logOutput, logger);
            _commandBuilder = new CommandBuilder(configuration);
        }

        public ICommand<WatermarkRequest> CreateWatermarkCommand()
        {
            return new WatermarkCommand(_executor, _commandBuilder);
        }

        //public ICommand<TrimVideoRequest> CreateTrimVideoCommand()
        //{
        //    return new TrimVideoCommand(_executor, _commandBuilder);
        //}

        //public ICommand<ConvertVideoRequest> CreateConvertVideoCommand()
        //{
        //    return new ConvertVideoCommand(_executor, _commandBuilder);
        //}

        //public ICommand<ExtractFrameRequest> CreateExtractFrameCommand()
        //{
        //    return new ExtractFrameCommand(_executor, _commandBuilder);
        //}
    }
}
