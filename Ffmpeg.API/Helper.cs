namespace FFmpeg.API
{
    public static class Helper
    {
        private static string GetOutputExtension(string format)
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

        private static string GetVideoCodec(string format)
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

        private static string GetAudioCodec(string format)
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

        private static string GetContentType(string format)
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

        public static bool IsValidTimeFormat(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return false;

            // Support formats: HH:MM:SS, MM:SS, or just SS
            var timeFormats = new[] { @"^\d{1,2}:\d{2}:\d{2}$", @"^\d{1,2}:\d{2}$", @"^\d+$" };
            
            foreach (var format in timeFormats)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(timeString, format))
                    return true;
            }
            
            return false;
        }

        public static string NormalizeTimeFormat(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return "00:00:00";

            // If it's just seconds (e.g., "30")
            if (System.Text.RegularExpressions.Regex.IsMatch(timeString, @"^\d+$"))
            {
                int seconds = int.Parse(timeString);
                int hours = seconds / 3600;
                int minutes = (seconds % 3600) / 60;
                int remainingSeconds = seconds % 60;
                return $"{hours:D2}:{minutes:D2}:{remainingSeconds:D2}";
            }

            // If it's MM:SS format
            if (System.Text.RegularExpressions.Regex.IsMatch(timeString, @"^\d{1,2}:\d{2}$"))
            {
                return $"00:{timeString}";
            }

            // If it's already HH:MM:SS format
            return timeString;
        }
    }
}
