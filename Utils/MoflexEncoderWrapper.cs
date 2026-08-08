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

            // Mobipeg Mobiclip encoder — ABSOLUTE MAXIMUM QUALITY settings.
            //
            // FACT: Mobiclip is a LOSSY codec. QP 12 is the encoder's hard minimum (QP < 12 = error).
            //       There is NO lossless mode for Mobiclip. Some generation loss is unavoidable.
            //       The settings below minimize that loss as much as physically possible.
            //
            // -mobiclip 1 -moflex 1  = Use Nintendo's custom quantization tables (REQUIRED)
            // -qp 12                 = Absolute minimum QP (highest quality). Lower values cause encoder error.
            // -mobi_qyx 0            = QY Extension Tier 0: preserves maximum chroma/luma detail
            // -motion-est tesa       = Exhaustive motion search (slowest but most accurate)
            // -preset veryslow       = Maximum compression effort
            // -mo_layout 4/6         = 3D SBS (4) or 2D (6) layout for 3DS playback
            string layoutArg = is3D ? "-mo_layout 4" : "-mo_layout 6";
            string qualityArgs = "-qp 12 -mobi_qyx 0 -motion-est tesa -preset veryslow";
            string args = $"-y -i \"{videoPath}\"";

            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
            {
                args += $" -i \"{audioPath}\"";
                args += $" -c:v mobiclip -mobiclip 1 -moflex 1 {layoutArg} {qualityArgs} -c:a adpcm_ima_mobiclip \"{outputPath}\"";
            }
            else
            {
                args += $" -c:v mobiclip -mobiclip 1 -moflex 1 {layoutArg} {qualityArgs} \"{outputPath}\"";
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

