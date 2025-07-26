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
            app.MapPost("/api/video/watermark", AddWatermark)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(104857600)); // 100 MB

            app.MapPost("/api/video/cut", CutVideo)
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(104857600)); // 100 MB
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
    }
}