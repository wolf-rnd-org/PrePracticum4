﻿using Microsoft.AspNetCore.Http;
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
            const int MaxUploadSize = 104857600;

            app.MapPost("/api/video/watermark", AddWatermark)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize)); 

            app.MapPost("/api/video/change-speed", ChangeVideoSpeed)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadSize));
        }

        private static async Task<IResult> AddWatermark(
            HttpContext context,
            [FromForm] WatermarkDto dto)
        {
            var fileService = context.RequestServices.GetRequiredService<IFileService>();
            var ffmpegService = context.RequestServices.GetRequiredService<IFFmpegServiceFactory>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>(); // or a specific logger type

            try
            {
                // Validate request
                if (dto.VideoFile == null || dto.WatermarkFile == null)
                {
                    return Results.BadRequest("Video file and watermark file are required");
                }

                // Save uploaded files
                string videoFileName = await fileService.SaveUploadedFileAsync(dto.VideoFile);
                string watermarkFileName = await fileService.SaveUploadedFileAsync(dto.WatermarkFile);

                // Generate output filename
                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFileName = await fileService.GenerateUniqueFileNameAsync(extension);

                // Track files to clean up
                List<string> filesToCleanup = new List<string> { videoFileName, watermarkFileName, outputFileName };

                try
                {
                    // Create and execute the watermark command
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

                    // Read the output file
                    byte[] fileBytes = await fileService.GetOutputFileAsync(outputFileName);

                    // Clean up temporary files
                    _ = fileService.CleanupTempFilesAsync(filesToCleanup);

                    // Return the file
                    return Results.File(fileBytes, "video/mp4", dto.VideoFile.FileName);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing watermark request");
                    // Clean up on error
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
    }
}
