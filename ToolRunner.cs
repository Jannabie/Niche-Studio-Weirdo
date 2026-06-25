using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NicheStudioWeirdo
{
    public static class ToolRunner
    {
        public static async Task RunAsync(string workingDirectory, string fileName, string arguments, MainWindow main)
        {
            try
            {
                main.LogToConsole($"▶ Executing: {fileName} {arguments}");

                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };

                // Both stdout and stderr are shown without [ERROR] prefix.
                // Python's logging module writes INFO/WARNING to stderr by default.
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        main.LogToConsole(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        main.LogToConsole(e.Data);   // no [ERROR] prefix here
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                    main.LogToConsole("✔ Command completed successfully.");
                else
                    main.LogToConsole($"✘ [ERROR] Command exited with code {process.ExitCode}.");
            }
            catch (Exception ex)
            {
                main.LogToConsole($"✘ [EXCEPTION] Failed to run tool: {ex.Message}");
            }
        }
    }
}
