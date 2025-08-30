using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FFmpeg.Core.Models;
using FFmpeg.Core.Interfaces;
using FFmpeg.API.DTOs;
using FFmpeg.Infrastructure.Services;

namespace FFmpeg.API.Endpoints
{
    public static class AudioEndpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapPost("/api/audio/mix", MixAudio)
                .DisableAntiforgery()
                .WithName("MixAudio")
                .Accepts<MixAudioDto>("multipart/form-data")
                .Produces<FileResult>(200)
                .WithMetadata(new RequestSizeLimitAttribute(104857600)); // עד 100MB
        }

        private static async Task<IResult> MixAudio(
            HttpContext context,
            [FromForm] MixAudioDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            if (dto.AudioFile1 == null || dto.AudioFile2 == null)
                return Results.BadRequest("שני קבצי האודיו נדרשים");

            var file1 = await fileService.SaveUploadedFileAsync(dto.AudioFile1);
            var file2 = await fileService.SaveUploadedFileAsync(dto.AudioFile2);
            var output = await fileService.GenerateUniqueFileNameAsync(".mp3");

            var filesToCleanup = new List<string> { file1, file2, output };

            try
            {
                var command = ffmpegService.CreateMixAudioCommand(file1, file2, output);
                var result = await command.RunAsync();

                if (!result.IsSuccess)
                {
                    logger.LogError("שגיאה בהרצת FFmpeg: {Error}", result.Error);
                    return Results.Problem("שגיאה בהרצת FFmpeg: " + result.Error);
                }

                var fileBytes = await fileService.GetOutputFileAsync(output);
                _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                return Results.File(fileBytes, "audio/mpeg", "mixed.mp3");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "שגיאה בזמן מיזוג קבצי אודיו");
                _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                return Results.Problem("שגיאה: " + ex.Message);
            }
        }
    }
}
