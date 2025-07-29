using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FFmpeg.API.DTOs;
using FFmpeg.Core.Interfaces;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace FFmpeg.API.Endpoints
{
    public static class VideoEndpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            // ----------- VIDEO ENDPOINT -----------
            const int MaxUploadSize = 104857600;

            app.MapPost("/api/video/watermark", AddWatermark)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize));

            // ----------- AUDIO ENDPOINT -----------
            app.MapPost("/api/audio/convert", ConvertAudio)
                .DisableAntiforgery()
                .WithName("ConvertAudio")
                .Accepts<ConvertAudioDto>("multipart/form-data");
            app.MapPost("/api/video/change-speed", ChangeVideoSpeed)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize));
            app.MapPost("/api/video/create-thumbnail", CreateThumbnail)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize));
            app.MapPost("/api/video/merge", MergeVideos)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(2 * MaxUploadSize)); // 200 MB for two videos
        }

        // ---------- VIDEO ----------
        private static async Task<IResult> AddWatermark(
            HttpContext context,
            [FromForm] WatermarkDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                if (dto.VideoFile == null || dto.WatermarkFile == null)
                {
                    return Results.BadRequest("Video file and watermark file are required");
                }

                string videoFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);
                string watermarkFileName = await fileService.SaveUploadedFileAsync(dto.WatermarkFile);

                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);

                List<string> filesToCleanup = new() { videoFileName, watermarkFileName, outputFileName };

                try
                {
                    var command = ffmpegService.CreateWatermarkCommand();
                    var result = await command.ExecuteAsync(new WatermarkModel
                    {
                        InputFile = videoFileName,
                        WatermarkFile = watermarkFileName,
                        OutputFile = outputFileName,
                        XPosition = dto.XPosition,
                        YPosition = dto.YPosition,
                        IsVideo = true,
                        VideoCodec = "libx264"
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
                            result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to add watermark: " + result.ErrorMessage, statusCode: 500);
                    }

                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    return Results.File(fileBytes, "video/mp4", dto.VideoFile.FileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing watermark request");
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AddWatermark endpoint");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }
        }

        // ---------- AUDIO ----------
        private static async Task<IResult> ConvertAudio(
            HttpContext context,
            [FromForm] ConvertAudioDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            if (dto.AudioFile == null || string.IsNullOrEmpty(dto.OutputFileName))
            {
                return Results.BadRequest("Audio file and output name are required");
            }

            string inputFileName = await fileService.SaveUploadedFileAsync(dto.AudioFile);
            string extension = Path.GetExtension(dto.OutputFileName);
            if (string.IsNullOrEmpty(extension))
            {
                return Results.BadRequest("Output file name must include extension (e.g., .wav)");
            }

            string outputFileName = dto.OutputFileName;
            List<string> filesToCleanup = new() { inputFileName, outputFileName };

            try
            {
                var command = ffmpegService.CreateConvertAudioCommand();
                var result = await command.ExecuteAsync(new ConvertAudioModel
                {
                    InputFile = inputFileName,
                    OutputFile = outputFileName
                });

                if (!result.IsSuccess)
                {
                    logger.LogError("Audio conversion failed: {Error}", result.ErrorMessage);
                    return Results.Problem("Audio conversion failed: " + result.ErrorMessage);
                }

                byte[] output = await fileService.GetOutputFileAsync(outputFileName);
                _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                return Results.File(output, "audio/wav", outputFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error converting audio");
                _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                return Results.Problem("Unexpected error: " + ex.Message);
            }
        }


        // החדש - Crop
        private static async Task<IResult> AddCrop(
            HttpContext context,
            [FromForm] CropDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                // בדיקות בסיסיות על הקלט
                if (dto.VideoFile == null)
                    return Results.BadRequest("Video file is required");

                if (string.IsNullOrWhiteSpace(dto.StartTime) || string.IsNullOrWhiteSpace(dto.EndTime))
                    return Results.BadRequest("StartTime and EndTime are required");

                if (string.IsNullOrWhiteSpace(dto.OutputFileName))
                    return Results.BadRequest("OutputFileName is required");

                // שמירת קובץ וידאו קלט
                string inputFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);

                // שמירת שם קובץ פלט כפי שנשלח (לפי הדרישה שלך)
                string outputFileName = dto.OutputFileName;

                var model = new CropModel
                {
                    InputFile = inputFileName,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    OutputFile = outputFileName
                };

                var command = ffmpegService.CreateCropCommand();
                var result = await command.ExecuteAsync(model);

                if (!result.IsSuccess)
                {
                    logger.LogError("FFmpeg Crop command failed: {ErrorMessage}, Command: {Command}",
                        result.ErrorMessage, result.CommandExecuted);
                    return Results.Problem("Failed to crop video: " + result.ErrorMessage, statusCode: 500);
                }

                var fileBytes = await fileService.GetOutputFileAsync(outputFileName);

                // ניקוי קבצים זמניים
                _ = fileService.CleanupTempFilesAsync(new[] { inputFileName, outputFileName });

                return Results.File(fileBytes, "video/mp4", outputFileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AddCrop endpoint");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }
        }
        private static async Task<IResult> ChangeVideoSpeed(
            HttpContext context,
            [FromForm] VideoSpeedChangeDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                if (dto.VideoFile == null || dto.Speed <= 0)
                {
                    return Results.BadRequest("Video file is required and speed factor must be positive");
                }

                string videoFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);
                string outputFileName = await fileService.GenerateUniqueFileNameAsync(".mp4");

                var command = ffmpegService.CreateVideoSpeedChangeCommand();
                if (command == null)
                {
                    logger.LogError("Failed to create FFmpeg command for changing video speed.");
                    return Results.Problem("Internal server error: Unable to process the request.", statusCode: 500);
                }

                var result = await command.ExecuteAsync(new ChangeSpeedModel
                {
                    InputFile = videoFileName,
                    Speed = dto.Speed,
                    OutputFile = outputFileName
                });

                if (!result.IsSuccess)
                {
                    logger.LogError("FFmpeg command failed: {ErrorMessage}", result.ErrorMessage);
                    return Results.Problem("Failed to change video speed: " + result.ErrorMessage, statusCode: 500);
                }

                byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);

                _ = fileService.CleanupTempFilesAsync(new List<string> { videoFileName, outputFileName });

                return Results.File(fileBytes, "video/mp4", dto.OutputFileName ?? dto.VideoFile.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing change speed request");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }
        }

        private static async Task<IResult> CreateThumbnail(
            HttpContext context,
            [FromForm] CreateThumbnailDTO dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                if (dto.VideoName == null || string.IsNullOrWhiteSpace(dto.ImageName))
                {
                    return Results.BadRequest("Video file and image name are required.");
                }

                if (!TimeSpan.TryParse(dto.Time, out var timestamp))
                {
                    return Results.BadRequest("Invalid time format. Use HH:mm:ss");
                }

                string inputFileName = await fileService.SaveUploadedFileAsync(dto.VideoName);

                string outputImageName = dto.ImageName.EndsWith(".jpg") ? dto.ImageName : dto.ImageName + ".jpg";

                List<string> filesToCleanup = new() { inputFileName, outputImageName };

                try
                {
                    var command = ffmpegService.CreateThumbnailCommand();
                    var result = await command.ExecuteAsync(new CreateThumbnailModel
                    {
                        VideoName = inputFileName,
                        ImageName = outputImageName,
                        Timestamp = timestamp
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg failed: {ErrorMessage}, Command: {Command}",
                            result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to create thumbnail: " + result.ErrorMessage, statusCode: 500);
                    }

                    byte[] imageBytes = await fileService.GetOutputFileAsync(outputImageName);
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    return Results.File(imageBytes, "image/jpeg", outputImageName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during thumbnail generation");
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    return Results.Problem("Internal error during thumbnail creation", statusCode: 500);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in CreateThumbnail");
                return Results.Problem("Unexpected error: " + ex.Message, statusCode: 500);
            }
        }


    private static async Task<IResult> MergeVideos(
        HttpContext context,
        [FromForm] MergeVideosDto dto)
    {
        var fileService = context.RequestServices.GetRequiredService<IFileService>();
        var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        try
        {
            // Validate request
            if (dto.VideoFile1 == null || dto.VideoFile2 == null)
            {
                return Results.BadRequest("Both video files are required for merging.");
            }

            // Save uploaded files
            string videoFile1Name = await fileService.SaveUploadedFileAsync(dto.VideoFile1);
            string videoFile2Name = await fileService.SaveUploadedFileAsync(dto.VideoFile2);

            // Generate output filename
            string extension = Path.GetExtension(dto.VideoFile1.FileName); // Use extension from first video
            string outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);

            // Track files to clean up
            List<string> filesToCleanup = new List<string> { videoFile1Name, videoFile2Name, outputFileName };

            try
            {
                // Create and execute the merge command
                var command = ffmpegService.CreateMergeVideosCommand();
                var result = await command.ExecuteAsync(new MergeVideosModel
                {
                    InputFile1 = videoFile1Name,
                    InputFile2 = videoFile2Name,
                    OutputFile = outputFileName,
                    Direction = dto.Direction
                });

                if (!result.IsSuccess)
                {
                    logger.LogError("FFmpeg merge command failed: {ErrorMessage}, Command: {Command}",
                        result.Message, result.Output);
                    return Results.Problem("Failed to merge videos: " + result.Message, statusCode: 500);
                }

                // Read the output file
                byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);

                // Clean up temporary files (fire and forget)
                _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                // Return the merged file
                return Results.File(fileBytes, "video/mp4", $"merged_video{extension}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing merge videos request");
                // Clean up on error
                _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                throw; // Re-throw to be caught by outer catch or global error handler
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in MergeVideos endpoint");
            return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
        }
    }


    }
}

