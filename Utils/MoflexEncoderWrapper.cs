using System;
using System.Diagnostics;
using System.IO;

namespace NicheStudioWeirdo.Utils
{
    public static class MoflexEncoderWrapper
    {
        public static void Encode(string videoPath, string audioPath, string moflexExePath, string outputPath, Action<string> logCallback)
        {
            if (!File.Exists(moflexExePath))
                throw new FileNotFoundException($"Moflex Encoder not found at {moflexExePath}");
            if (!File.Exists(videoPath))
                throw new FileNotFoundException($"Input video not found at {videoPath}");

            // Basic arguments for standard Moflex SDK Encoder
            // This might vary depending on the exact version of the SDK leak
            string args = $"-i \"{videoPath}\"";
            
            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                args += $" -snd \"{audioPath}\""; // Some versions use -snd or -audio
            }

            args += $" -o \"{outputPath}\"";

            logCallback($"Running Moflex Encoder with arguments: {args}");

            var startInfo = new ProcessStartInfo
            {
                FileName = moflexExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logCallback(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logCallback("ERROR: " + e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    logCallback($"Moflex Encoder exited with code {process.ExitCode}");
                }
            }
        }
    }
}
