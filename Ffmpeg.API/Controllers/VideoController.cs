using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFmpeg.API.DTOs;
using FFmpeg.Core.Interfaces;
using FFmpeg.Core.Models;
using FFmpeg.Infrastructure.Services;

namespace FFmpeg.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IFFmpegServiceFactory _ffmpegFactory;
        private readonly IFileService _fileService;
        private readonly ILogger<VideoController> _logger;

        public VideoController(
            IFFmpegServiceFactory ffmpegFactory,
            IFileService fileService,
            ILogger<VideoController> logger)
        {
            _ffmpegFactory = ffmpegFactory;
            _fileService = fileService;
            _logger = logger;
        }

        [HttpPost("watermark")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> AddWatermark([FromForm] WatermarkDto dto)
        {
            try
            {
                // Validate request
                if (dto.VideoFile == null || dto.WatermarkFile == null)
                {
                    return BadRequest("Video file and watermark file are required");
                }

                // Save uploaded files
                string videoFileName = await _fileService.SaveUploadedFileAsync(dto.VideoFile);
                string watermarkFileName = await _fileService.SaveUploadedFileAsync(dto.WatermarkFile);

                // Generate output filename
                string extension = Path.GetExtension(dto.VideoFile.FileName);
                string outputFileName = await _fileService.GenerateUniqueFileNameAsync(extension);

                // Track files to clean up
                List<string> filesToCleanup = new List<string> { videoFileName, watermarkFileName, outputFileName };

                try
                {
                    // Create and execute the watermark command
                    var command = _ffmpegFactory.CreateWatermarkCommand();
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
                        _logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
                            result.ErrorMessage, result.CommandExecuted);
                        return StatusCode(500, "Failed to add watermark: " + result.ErrorMessage);
                    }

                    // Read the output file
                    byte[] fileBytes = await _fileService.GetOutputFileAsync(outputFileName);

                    // Clean up temporary files
                    _ = _fileService.CleanupTempFilesAsync(filesToCleanup);

                    // Return the file
                    return File(fileBytes, "video/mp4", dto.VideoFile.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing watermark request");
                    // Clean up on error
                    _ = _fileService.CleanupTempFilesAsync(filesToCleanup);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddWatermark endpoint");
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }


        #region Helper Methods

        private string GetOutputExtension(string format)
        {
            return format?.ToLowerInvariant() switch
            {
                "mp4" => ".mp4",
                "webm" => ".webm",
                "mkv" => ".mkv",
                "avi" => ".avi",
                "mov" => ".mov",
                "gif" => ".gif",
                _ => ".mp4" // Default to MP4
            };
        }

        private string GetVideoCodec(string format)
        {
            return format?.ToLowerInvariant() switch
            {
                "mp4" => "libx264",
                "webm" => "libvpx-vp9",
                "mkv" => "libx264",
                "avi" => "libx264",
                "mov" => "libx264",
                "gif" => "gif",
                _ => "libx264" // Default to H.264
            };
        }

        private string GetAudioCodec(string format)
        {
            return format?.ToLowerInvariant() switch
            {
                "mp4" => "aac",
                "webm" => "libopus",
                "mkv" => "aac",
                "avi" => "aac",
                "mov" => "aac",
                "gif" => "copy", // GIF has no audio
                _ => "aac" // Default to AAC
            };
        }

        private string GetContentType(string format)
        {
            return format?.ToLowerInvariant() switch
            {
                "mp4" => "video/mp4",
                "webm" => "video/webm",
                "mkv" => "video/x-matroska",
                "avi" => "video/x-msvideo",
                "mov" => "video/quicktime",
                "gif" => "image/gif",
                _ => "video/mp4" // Default to MP4
            };
        }

        #endregion
    }

    
}