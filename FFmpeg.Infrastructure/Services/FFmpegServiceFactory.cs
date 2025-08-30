using Ffmpeg.Command;
using Ffmpeg.Command.Commands;
using FFmpeg.Core.Interfaces;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FFmpeg.Infrastructure.Services
{
    public interface IFFmpegServiceFactory
    {
        ICommand<WatermarkModel> CreateWatermarkCommand();
        ICommand<MergeVideosModel> CreateMergeVideosCommand();
        ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand();
        ICommand<ConvertAudioModel> CreateConvertAudioCommand();

        ICommand<CreateThumbnailModel> CreateThumbnailCommand();
        ICommand<CropModel> CreateCropCommand();
        ICommand<VideoCuttingModel> CreateVideoCuttingCommand();
        
        ICommand<RotationModel> CreateRotationCommand();
        ICommand<GreenScreenModel> CreateGreenScreenCommand();
        ICommandRunner CreateMixAudioCommand(string input1, string input2, string output);
        ICommand<ColorFilterModel> CreateColorFilterCommand();
        ICommand<VideoCompreesinModel> ChangeVideoCompressionCommand();
        ICommand<ChangeVolumeModel> CreateVolumeChangeCommand();
    }

    public class FFmpegServiceFactory : IFFmpegServiceFactory
    {
        private readonly FFmpegExecutor _executor;
        private readonly ICommandBuilder _commandBuilder;

        public FFmpegServiceFactory(IConfiguration configuration, ILogger? logger = null)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegPath = Path.Combine(baseDirectory, "external", "ffmpeg.exe");

            bool logOutput = bool.TryParse(configuration["FFmpeg:LogOutput"], out bool log) && log;

            _executor = new FFmpegExecutor(ffmpegPath, logOutput, logger!);
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
        public ICommand<MergeVideosModel> CreateMergeVideosCommand()
        {
            return new MergeVideosCommand(_executor, _commandBuilder);
        }
        public ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand()
        {
            return new ChangeSpeedCommand(_executor, _commandBuilder);
        }
        public ICommandRunner CreateMixAudioCommand(string input1, string input2, string output)
        {
            return new MixAudioCommand(input1, input2, output);
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
        public ICommand<ConvertAudioModel> CreateConvertAudioCommand()
        {
            return new ConvertAudioCommand(_executor, _commandBuilder);
        }
       public ICommand<RotationModel> CreateRotationCommand()
        {
            return new RotationCommand(_executor, _commandBuilder);
        }
        public ICommand<VideoCompreesinModel> ChangeVideoCompressionCommand()
        {
            return new VideoCompressionCommand(_executor, _commandBuilder);
        }
        public ICommand<GreenScreenModel> CreateGreenScreenCommand()
        {
            return new GreenScreenCommand(_executor, _commandBuilder);
        }
        public ICommand<ChangeVolumeModel> CreateVolumeChangeCommand()
        {
            return new ChangeVolumeCommand(_executor, _commandBuilder);
        }
    }
}