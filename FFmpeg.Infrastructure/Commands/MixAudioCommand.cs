using FFmpeg.Core.Interfaces;
using FFmpeg.Core.Models;
using System.Diagnostics;

namespace FFmpeg.Infrastructure.Commands
{
    public class MixAudioCommand : ICommandRunner
    {
        private readonly string _input1;
        private readonly string _input2;
        private readonly string _output;

        public MixAudioCommand(string input1, string input2, string output)
        {
            _input1 = input1;
            _input2 = input2;
            _output = output;
        }

        public async Task<Result> RunAsync()
        {
            var arguments = $"-i {_input1} -i {_input2} -filter_complex \"amix=inputs=2\" {_output}";

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return Result.Failure($"FFmpeg error: {error}");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Exception: {ex.Message}");
            }
        }
    }
}
