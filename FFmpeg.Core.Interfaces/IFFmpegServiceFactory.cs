public interface IFFmpegServiceFactory
{
    ICommand<WatermarkModel> CreateWatermarkCommand();
    ICommand<CropModel> CreateCropCommand();
    ICommand<ChangeSpeedModel> CreateVideoSpeedChangeCommand(); // <-- Fix here
}