namespace FFmpeg.Infrastructure.Commands
{
    public class CommandResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string CommandExecuted { get; set; } = string.Empty;
        public string OutputLog { get; set; } = string.Empty;
    }
}
