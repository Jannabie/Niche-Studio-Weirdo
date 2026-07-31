using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NicheStudioWeirdo
{
    public static class ToolRunner
    {
        /// <summary>
        /// Run a tool with a raw arguments string (legacy). Avoid for paths with special chars.
        /// </summary>
        public static async Task<int> RunAsync(string workingDirectory, string fileName, string arguments, MainWindow main)
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
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                psi.EnvironmentVariables["PYTHONUTF8"] = "1";

                return await StartAndWaitAsync(psi, main);
            }
            catch (Exception ex)
            {
                main.LogToConsole($"✘ [EXCEPTION] Failed to run tool: {ex.Message}");
                return -1;
            }
        }

        public static async Task<int> RunAsync(string workingDirectory, string fileName, IEnumerable<string> args, MainWindow main)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                psi.EnvironmentVariables["PYTHONUTF8"] = "1";

                foreach (var a in args)
                    psi.ArgumentList.Add(a);

                main.LogToConsole($"▶ Executing: {fileName} {string.Join(" ", psi.ArgumentList)}");

                return await StartAndWaitAsync(psi, main);
            }
            catch (Exception ex)
            {
                main.LogToConsole($"✘ [EXCEPTION] Failed to run tool: {ex.Message}");
                return -1;
            }
        }

        private static async Task<int> StartAndWaitAsync(ProcessStartInfo psi, MainWindow main)
        {
            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    main.LogToConsole(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    main.LogToConsole(e.Data);
            };

            process.Start();
            
            try 
            { 
                process.StandardInput.WriteLine();
                process.StandardInput.Close(); 
            } 
            catch { }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
                main.LogToConsole("✔ Command completed successfully.");
            else
                main.LogToConsole($"✘ [ERROR] Command exited with code {process.ExitCode}.");

            return process.ExitCode;
        }
    }
}
