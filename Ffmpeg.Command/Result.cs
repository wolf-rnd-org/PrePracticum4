namespace Ffmpeg.Command
{
    public class Result
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string CommandExecuted { get; set; }
        public string OutputLog { get; set; }
    }
}
