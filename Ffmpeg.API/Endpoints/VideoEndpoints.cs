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
        private const int MaxUploadSize = 104857600; // 100 MB

        public static void MapEndpoints(this WebApplication app)
        {
            // ----------- VIDEO ENDPOINT -----------
            const int MaxUploadSize = 104857600;

            app.MapPost("/api/video/watermark", AddWatermark)
                .DisableAntiforgery()
              
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize)); // 100 MB

            app.MapPost("/api/video/rotation", AddRotation)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize)); 

                //.WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize));
            app.MapPost("/api/video/color-filter", ApplyColorFilter)
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

            app.MapPost("/api/video/cut", CutVideo)
               .DisableAntiforgery()
               .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize)); // 100 MB

            app.MapPost("/api/video/compress", CompressVideo)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize)); // 100MB

            app.MapPost("/api/video/greenscreen", ApplyGreenScreen)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize)); // 100 MB

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

        private static async Task<IResult> AddRotation(
        HttpContext context,
        [FromForm] RotationDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>(); // or a specific logger type

            try
            {
                // Validate request
                if (dto.InputFile == null || dto.Angle == 0)
                {
                    return Results.BadRequest("Video file and rotation angle are required");
                }

                // Save uploaded video file
                string videoFileName = await fileService.SaveUploadedFileAsync(dto.InputFile);


                // Generate output filename
                string extension = Path.GetExtension(dto.InputFile.FileName);
                string outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);

                // Track files to clean up
                List<string> filesToCleanup = new List<string> { videoFileName, outputFileName };

                try
                {
                    // Create and execute the rotate command
                    var command = ffmpegService.CreateRotationCommand();
                    var result = await command.ExecuteAsync(new RotationModel
                    {
                        InputFile = videoFileName,
                        Angle = dto.Angle,
                        OutputFile = outputFileName
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {CommandExecuted}",
                            result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to rotate video: " + result.ErrorMessage, statusCode: 500);
                    }

                    // Read the output file
                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);

                    // Clean up temporary files
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    // Return the file
                    return Results.File(fileBytes, "video/mp4", dto.InputFile.FileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing rotate request");
                    // Clean up on error
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RotateVideo endpoint");
                              return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }

        }

        private static async Task<IResult> ApplyColorFilter(
        HttpContext context,
        [FromForm] ColorFilterDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            try
            {
                if (dto.VideoFile == null)
                {
                    return Results.BadRequest("Video file is required");
                }

                string inputFile = await fileService.SaveUploadedFileAsync(dto.VideoFile);
                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFile = await fileService.GenerateUniqueFileNameAsync(extension);

                List<string> filesToCleanup = new() { inputFile, outputFile };


                try
                {
                    var command = ffmpegService.CreateColorFilterCommand();
                    var result = await command.ExecuteAsync(new ColorFilterModel
                    {
                        InputFile = inputFile,
                        OutputFile = outputFile,
                        IsVideo = true,
                        VideoCodec = "libx264"
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
                            result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to apply color filter: " + result.ErrorMessage, statusCode: 500);
                    }

                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFile);
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    return Results.File(fileBytes, "video/mp4", dto.VideoFile.FileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error applying color filter");
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ApplyColorFilter endpoint");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
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

                var command = ffmpegService.CreateWatermarkCommand();
                if (command == null)
                {
                    logger.LogError("Failed to create FFmpeg command for changing video speed.");
                    return Results.Problem("Internal server error: Unable to process the request.", statusCode: 500);
                }

                var result = await command.ExecuteAsync(new WatermarkModel
                {
                    InputFile = videoFileName,
                    
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

        private static async Task<IResult> CutVideo(
            HttpContext context,
            [FromForm] VideoCuttingDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                // Validate request
                if (dto.VideoFile == null)
                {
                    return Results.BadRequest("Video file is required");
                }

                if (string.IsNullOrEmpty(dto.StartTime))
                {
                    return Results.BadRequest("Start time is required");
                }

                if (string.IsNullOrEmpty(dto.EndTime))
                {
                    return Results.BadRequest("End time is required");
                }

                // Save uploaded file
                string videoFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);

                // Generate output filename
                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFileName;

                if (!string.IsNullOrWhiteSpace(dto.OutputName))
                {
                    // Use custom name provided by user
                    outputFileName = dto.OutputName + extension;
                }
                else
                {
                    // Use original name with _cut suffix  
                    outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);
                }

                // Track files to clean up
                List<string> filesToCleanup = new List<string> { videoFileName, outputFileName };

                try
                {
                    // Create and execute the video cutting command
                    var command = ffmpegService.CreateVideoCuttingCommand();
                    var result = await command.ExecuteAsync(new VideoCuttingModel
                    {
                        InputFile = videoFileName,
                        OutputFile = outputFileName,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
                            result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to cut video: " + result.ErrorMessage, statusCode: 500);
                    }

                    // Read the output file
                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);

                    // Clean up temporary files
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    // Return the file
                    string returnFileName;
                    if (!string.IsNullOrWhiteSpace(dto.OutputName))
                    {
                        returnFileName = dto.OutputName + extension;
                    }
                    else
                    {
                        string originalFileName = Path.GetFileNameWithoutExtension(dto.VideoFile.FileName);
                        returnFileName = $"{originalFileName}_cut{extension}";
                    }

                    return Results.File(fileBytes, "video/mp4", returnFileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing video cutting request");
                    // Clean up on error
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in CutVideo endpoint");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }
        }
        private static async Task<IResult> CompressVideo(
    HttpContext context,
    [FromForm] VideoCompreesionDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                if (dto.VideoFile == null)
                    return Results.BadRequest("Video file is required");

                string videoFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);
                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);
                List<string> filesToCleanup = new() { videoFileName, outputFileName };

                try
                {
                    var command = ffmpegService.ChangeVideoCompressionCommand();
                    var result = await command.ExecuteAsync(new VideoCompreesinModel
                    {
                        InputFile = videoFileName,
                        OutputFile = outputFileName,
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}", result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to compress video: " + result.ErrorMessage, statusCode: 500);
                    }

                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    return Results.File(fileBytes, "video/mp4", dto.VideoFile.FileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing compress video request");
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in CompressVideo endpoint");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }
        }

        private static async Task<IResult> ApplyGreenScreen(
            HttpContext context,
            [FromForm] GreenScreenDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                if (dto.VideoFile == null || dto.BackgroundFile == null)
                    return Results.BadRequest("Video file and background file are required");

                string videoFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);
                string backgroundFileName = await fileService.SaveUploadedFileAsync(dto.BackgroundFile);
                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);
                var filesToCleanup = new List<string> { videoFileName, backgroundFileName, outputFileName };

                try
                {
                    var command = ffmpegService.CreateGreenScreenCommand();
                    var result = await command.ExecuteAsync(new GreenScreenModel
                    {
                        InputFile = videoFileName,
                        BackgroundFile = backgroundFileName,
                        OutputFile = outputFileName,
                        VideoCodec = "libx264"
                    });

                    if (!result.IsSuccess)
                    {
                        logger.LogError("FFmpeg GreenScreen failed: {ErrorMessage}, Command: {Command}",
                            result.ErrorMessage, result.CommandExecuted);
                        return Results.Problem("Failed to process green screen: " + result.ErrorMessage, statusCode: 500);
                    }

                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    return Results.File(fileBytes, "video/mp4", dto.VideoFile.FileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing green screen");
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GreenScreen endpoint");
                return Results.Problem("An error occurred: " + ex.Message, statusCode: 500);
            }
        }
    }
}