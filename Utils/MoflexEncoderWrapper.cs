using System;
using System.Diagnostics;
using System.IO;

namespace NicheStudioWeirdo.Utils
{
    /// <summary>
    /// Uses Mobipeg (open-source patched FFmpeg) to encode MP4 → Moflex.
    /// No Nintendo SDK required. mobipeg.exe must be in the Tools folder.
    /// Project: https://github.com/quatric/mobipeg
    /// </summary>
    public static class MoflexEncoderWrapper
    {
        public static void Encode(string videoPath, string audioPath, string mobipegPath, string outputPath, Action<string> logCallback)
        {
            if (!File.Exists(mobipegPath))
                throw new FileNotFoundException($"mobipeg.exe not found at {mobipegPath}");
            if (!File.Exists(videoPath))
                throw new FileNotFoundException($"Input video not found at {videoPath}");

            // Mobipeg is a patched FFmpeg. Encoding command:
            // ffmpeg -i video.mp4 [-i audio.wav] -c:v mobiclip [-c:a adpcm_ima_mobiclip] -y output.moflex
            string args = $"-y -i \"{videoPath}\"";

            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                args += $" -i \"{audioPath}\"";
                args += $" -c:v mobiclip -c:a adpcm_ima_mobiclip \"{outputPath}\"";
            }
            else
            {
                args += $" -c:v mobiclip \"{outputPath}\"";
            }

            logCallback($"[Mobipeg] Running: mobipeg.exe {args}");

            var startInfo = new ProcessStartInfo
            {
                FileName = mobipegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logCallback(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) logCallback(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    logCallback($"[Mobipeg] Process exited with code {process.ExitCode}");
            }
        }
    }
}

