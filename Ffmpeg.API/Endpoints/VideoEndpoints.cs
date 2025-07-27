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
            app.MapPost("/api/video/watermark", AddWatermark)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(104857600)); // 100 MB

            // Merge Videos endpoint
            app.MapPost("/api/video/merge", MergeVideos)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(209715200)); // 200 MB for two videos
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

        // MergeVideos method
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