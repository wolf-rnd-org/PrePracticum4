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
            // ----------- VIDEO ENDPOINT -----------
            app.MapPost("/api/video/watermark", AddWatermark)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(104857600)); // 100 MB

            // ----------- AUDIO ENDPOINT -----------
            app.MapPost("/api/audio/convert", ConvertAudio)
                .DisableAntiforgery()
                .WithName("ConvertAudio")
                .Accepts<ConvertAudioDto>("multipart/form-data");
            app.MapPost("/api/video/change-speed", ChangeVideoSpeed)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(104857600)); // 100MB
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
<<<<<<< HEAD

=======
>>>>>>> c0293682b95b5f55feb084d63ef3fff3d646f75f
