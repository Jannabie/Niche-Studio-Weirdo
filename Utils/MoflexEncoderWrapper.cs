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
        public static void Encode(string videoPath, string audioPath, string mobipegPath, string outputPath, bool is3D, Action<string> logCallback)
        {
            if (!File.Exists(mobipegPath))
                throw new FileNotFoundException($"mobipeg.exe not found at {mobipegPath}");
            if (!File.Exists(videoPath))
                throw new FileNotFoundException($"Input video not found at {videoPath}");

            // Mobipeg is a patched FFmpeg. Encoding command:
            // Use QP 12 (highest quality, absolute max the 3DS supports) and preset veryslow for best compression and no pixelation.
            // CRITICAL: Must use -mobiclip 1 -moflex 1 to use Nintendo's custom quantization tables!
            // If the video is 3D (Side-by-Side), we MUST specify -mo_layout 4, otherwise the 3DS squashes it to 2D.
            string layoutArg = is3D ? "-mo_layout 4" : "-mo_layout 6";
            string args = $"-y -i \"{videoPath}\"";

            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                args += $" -i \"{audioPath}\"";
                args += $" -c:v mobiclip -mobiclip 1 -moflex 1 {layoutArg} -qp 12 -preset veryslow -c:a adpcm_ima_mobiclip \"{outputPath}\"";
            }
            else
            {
                args += $" -c:v mobiclip -mobiclip 1 -moflex 1 {layoutArg} -qp 12 -preset veryslow \"{outputPath}\"";
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

            // Force Mobipeg into absolute highest quality by disabling skip blocks and using max subme
            startInfo.EnvironmentVariables["MOBI_SKIP"] = "0";
            startInfo.EnvironmentVariables["MOBI_SUBME"] = "9";

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

