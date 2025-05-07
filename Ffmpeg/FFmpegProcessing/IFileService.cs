namespace Ffmpeg.API.FFmpegProcessing
{
    public interface IFileService
    {
        Task<string> SaveUploadedFileAsync(IFormFile file);
        string GetFullInputPath(string fileName);
        string GetFullOutputPath(string fileName);
        string GetFullTempPath(string fileName);
        Task<byte[]> GetOutputFileAsync(string fileName);
        Task CleanupTempFilesAsync(IEnumerable<string> fileNames);
        Task<string> GenerateUniqueFileNameAsync(string extension);
    }
}
