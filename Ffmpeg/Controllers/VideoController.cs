using Ffmpeg.API.FFmpegProcessing;
using Ffmpeg.Command.Requests;
using Ffmpeg.Command;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FFmpegApi.Controllers
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
                    var result = await command.ExecuteAsync(new WatermarkRequest
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

        //[HttpPost("trim")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[RequestSizeLimit(104857600)] // 100 MB
        //public async Task<IActionResult> TrimVideo([FromForm] TrimVideoDto dto)
        //{
        //    try
        //    {
        //        // Validate request
        //        if (dto.VideoFile == null)
        //        {
        //            return BadRequest("Video file is required");
        //        }

        //        // Save uploaded file
        //        string videoFileName = await _fileService.SaveUploadedFileAsync(dto.VideoFile);

        //        // Generate output filename
        //        string extension = Path.GetExtension(dto.VideoFile.FileName);
        //        string outputFileName = await _fileService.GenerateUniqueFileNameAsync(extension);

        //        // Track files to clean up
        //        List<string> filesToCleanup = new List<string> { videoFileName, outputFileName };

        //        try
        //        {
        //            // Create and execute the trim command
        //            var command = _ffmpegFactory.CreateTrimVideoCommand();
        //            var result = await command.ExecuteAsync(new TrimVideoRequest
        //            {
        //                InputFile = videoFileName,
        //                OutputFile = outputFileName,
        //                StartTime = TimeSpan.FromSeconds(dto.StartTimeSeconds),
        //                Duration = TimeSpan.FromSeconds(dto.DurationSeconds),
        //                VideoCodec = "libx264",
        //                AudioCodec = "aac"
        //            });

        //            if (!result.IsSuccess)
        //            {
        //                _logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
        //                    result.ErrorMessage, result.CommandExecuted);
        //                return StatusCode(500, "Failed to trim video: " + result.ErrorMessage);
        //            }

        //            // Read the output file
        //            byte[] fileBytes = await _fileService.GetOutputFileAsync(outputFileName);

        //            // Clean up temporary files
        //            _ = _fileService.CleanupTempFilesAsync(filesToCleanup);

        //            // Return the file
        //            return File(fileBytes, "video/mp4", dto.VideoFile.FileName);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error processing trim request");
        //            // Clean up on error
        //            _ = _fileService.CleanupTempFilesAsync(filesToCleanup);
        //            throw;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in TrimVideo endpoint");
        //        return StatusCode(500, "An error occurred: " + ex.Message);
        //    }
        //}

        //[HttpPost("extract-frame")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[RequestSizeLimit(104857600)] // 100 MB
        //public async Task<IActionResult> ExtractFrame([FromForm] ExtractFrameDto dto)
        //{
        //    try
        //    {
        //        // Validate request
        //        if (dto.VideoFile == null)
        //        {
        //            return BadRequest("Video file is required");
        //        }

        //        // Save uploaded file
        //        string videoFileName = await _fileService.SaveUploadedFileAsync(dto.VideoFile);

        //        // Generate output filename
        //        string outputFileName = await _fileService.GenerateUniqueFileNameAsync(".jpg");

        //        // Track files to clean up
        //        List<string> filesToCleanup = new List<string> { videoFileName, outputFileName };

        //        try
        //        {
        //            // Create and execute the extract frame command
        //            var command = _ffmpegFactory.CreateExtractFrameCommand();
        //            var result = await command.ExecuteAsync(new ExtractFrameRequest
        //            {
        //                InputFile = videoFileName,
        //                OutputFile = outputFileName,
        //                TimePosition = TimeSpan.FromSeconds(dto.TimePositionSeconds)
        //            });

        //            if (!result.IsSuccess)
        //            {
        //                _logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
        //                    result.ErrorMessage, result.CommandExecuted);
        //                return StatusCode(500, "Failed to extract frame: " + result.ErrorMessage);
        //            }

        //            // Read the output file
        //            byte[] fileBytes = await _fileService.GetOutputFileAsync(outputFileName);

        //            // Clean up temporary files
        //            _ = _fileService.CleanupTempFilesAsync(filesToCleanup);

        //            // Return the file
        //            return File(fileBytes, "image/jpeg", Path.GetFileNameWithoutExtension(dto.VideoFile.FileName) + "_frame.jpg");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error processing extract frame request");
        //            // Clean up on error
        //            _ = _fileService.CleanupTempFilesAsync(filesToCleanup);
        //            throw;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in ExtractFrame endpoint");
        //        return StatusCode(500, "An error occurred: " + ex.Message);
        //    }
        //}

        //[HttpPost("convert")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[RequestSizeLimit(104857600)] // 100 MB
        //public async Task<IActionResult> ConvertVideo([FromForm] ConvertVideoDto dto)
        //{
        //    try
        //    {
        //        // Validate request
        //        if (dto.VideoFile == null)
        //        {
        //            return BadRequest("Video file is required");
        //        }

        //        // Save uploaded file
        //        string videoFileName = await _fileService.SaveUploadedFileAsync(dto.VideoFile);

        //        // Determine output format and generate output filename
        //        string outputExtension = GetOutputExtension(dto.OutputFormat);
        //        string outputFileName = await _fileService.GenerateUniqueFileNameAsync(outputExtension);

        //        // Track files to clean up
        //        List<string> filesToCleanup = new List<string> { videoFileName, outputFileName };

        //        try
        //        {
        //            // Create and execute the convert command
        //            var command = _ffmpegFactory.CreateConvertVideoCommand();
        //            var result = await command.ExecuteAsync(new ConvertVideoRequest
        //            {
        //                InputFile = videoFileName,
        //                OutputFile = outputFileName,
        //                VideoCodec = GetVideoCodec(dto.OutputFormat),
        //                AudioCodec = GetAudioCodec(dto.OutputFormat),
        //                Width = dto.Width,
        //                Height = dto.Height,
        //                FrameRate = dto.FrameRate,
        //                VideoQuality = dto.Quality ?? 23 // Default CRF value
        //            });

        //            if (!result.IsSuccess)
        //            {
        //                _logger.LogError("FFmpeg command failed: {ErrorMessage}, Command: {Command}",
        //                    result.ErrorMessage, result.CommandExecuted);
        //                return StatusCode(500, "Failed to convert video: " + result.ErrorMessage);
        //            }

        //            // Read the output file
        //            byte[] fileBytes = await _fileService.GetOutputFileAsync(outputFileName);

        //            // Clean up temporary files
        //            _ = _fileService.CleanupTempFilesAsync(filesToCleanup);

        //            // Return the file
        //            string filename = Path.GetFileNameWithoutExtension(dto.VideoFile.FileName) + outputExtension;
        //            string contentType = GetContentType(dto.OutputFormat);
        //            return File(fileBytes, contentType, filename);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error processing convert request");
        //            // Clean up on error
        //            _ = _fileService.CleanupTempFilesAsync(filesToCleanup);
        //            throw;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in ConvertVideo endpoint");
        //        return StatusCode(500, "An error occurred: " + ex.Message);
        //    }
        //}

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

    #region DTO Classes

    public class WatermarkDto
    {
        public IFormFile VideoFile { get; set; }
        public IFormFile WatermarkFile { get; set; }
        public int XPosition { get; set; } = 10;
        public int YPosition { get; set; } = 10;
    }

    public class TrimVideoDto
    {
        public IFormFile VideoFile { get; set; }
        public double StartTimeSeconds { get; set; } = 0;
        public double DurationSeconds { get; set; } = 30;
    }

    public class ExtractFrameDto
    {
        public IFormFile VideoFile { get; set; }
        public double TimePositionSeconds { get; set; } = 0;
    }

    public class ConvertVideoDto
    {
        public IFormFile VideoFile { get; set; }
        public string OutputFormat { get; set; } = "mp4";
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? FrameRate { get; set; }
        public int? Quality { get; set; } // CRF value (lower is better)
    }

    #endregion
}