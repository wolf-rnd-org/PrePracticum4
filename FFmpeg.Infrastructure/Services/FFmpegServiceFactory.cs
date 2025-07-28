using Ffmpeg.Command;
using Ffmpeg.Command.Commands;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.Infrastructure.Services
{
    public interface IFFmpegServiceFactory
    {
        ICommand<WatermarkModel> CreateWatermarkCommand();
<<<<<<< HEAD
        ICommand<MergeVideosModel> CreateMergeVideosCommand();
=======
        ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand();
>>>>>>> c0293682b95b5f55feb084d63ef3fff3d646f75f
    }

    public class FFmpegServiceFactory : IFFmpegServiceFactory
    {
        private readonly FFmpegExecutor _executor;
        private readonly ICommandBuilder _commandBuilder;

        public FFmpegServiceFactory(IConfiguration configuration, ILogger logger = null)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = Path.Combine(baseDirectory, "external", "ffmpeg.exe");

            bool logOutput = bool.TryParse(configuration["FFmpeg:LogOutput"], out bool log) && log;

            _executor = new FFmpegExecutor(ffmpegPath, logOutput, logger);
            _commandBuilder = new CommandBuilder(configuration);
        }

        public ICommand<WatermarkModel> CreateWatermarkCommand()
        {
            return new WatermarkCommand(_executor, _commandBuilder);
        }
<<<<<<< HEAD
        // MergeVideos
        public ICommand<MergeVideosModel> CreateMergeVideosCommand()
        {
            return new MergeVideosCommand(_executor, _commandBuilder);
=======

        public ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand()
        {
            return new ChangeSpeedCommand(_executor, _commandBuilder);
>>>>>>> c0293682b95b5f55feb084d63ef3fff3d646f75f
        }
    }
}
