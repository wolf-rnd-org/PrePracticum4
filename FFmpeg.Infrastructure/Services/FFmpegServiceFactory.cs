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

namespace FFmpeg.Infrastructure.Services
{
    public interface IFFmpegServiceFactory
    {
        ICommand<WatermarkModel> CreateWatermarkCommand();


        ICommand<CreatePreviewModel> CreatePreviewCommand();

        ICommand<CreateThumbnailModel> CreateThumbnailCommand();

        ICommand<CropModel> CreateCropCommand();
        ICommandRunner CreateMixAudioCommand(string input1, string input2, string output);
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
        // using FFmpeg.Infrastructure.Commands;
        public ICommandRunner CreateMixAudioCommand(string input1, string input2, string output)
        {
            return new MixAudioCommand(input1, input2, output);
        }


        public ICommand<CreateThumbnailModel> CreateThumbnailCommand()
        {
            return new CreateThumbnailCommand(_executor, _commandBuilder);
        }
        public ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand()
        {
            throw new NotImplementedException();
        }
    }
}
