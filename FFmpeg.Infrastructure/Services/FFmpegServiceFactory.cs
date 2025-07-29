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
        ICommand<ColorFilterModel> CreateColorFilterCommand();
        ICommand<VideoCuttingModel> CreateVideoCuttingCommand();
        ICommand<CreateThumbnailModel> CreateThumbnailCommand();
        ICommand<GreenScreenModel> CreateGreenScreenCommand();
        ICommand<CropModel> CreateCropCommand();
        ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand();
        ICommand<ConvertAudioModel> CreateConvertAudioCommand();
        ICommand<VideoCompreesinModel> ChangeVideoCompressionCommand();

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

        public ICommand<CropModel> CreateCropCommand()
        {
            throw new NotImplementedException();
        }
        public ICommand<WatermarkModel> CreateWatermarkCommand()
        {
            return new WatermarkCommand(_executor, _commandBuilder);
        }
        public ICommand<ColorFilterModel> CreateColorFilterCommand()
        {
            return new ColorFilterCommand(_executor, _commandBuilder);
        }

        public ICommand<VideoCuttingModel> CreateVideoCuttingCommand()
        {
            return new VideoCuttingCommand(_executor, _commandBuilder);
        }

        public ICommand<CreateThumbnailModel> CreateThumbnailCommand()
        {
            return new CreateThumbnailCommand(_executor, _commandBuilder);
        }
        public ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand()
        {
            throw new NotImplementedException();
        }

        public ICommand<ConvertAudioModel> CreateConvertAudioCommand()
        {
            return new ConvertAudioCommand(_executor, _commandBuilder);
        }

        public ICommand<VideoCompreesinModel> ChangeVideoCompressionCommand()
        {
            return new VideoCompressionCommand(_executor, _commandBuilder);
        }

        public ICommand<GreenScreenModel> CreateGreenScreenCommand()
        {
            return new GreenScreenCommand(_executor, _commandBuilder);
        }
    }
}