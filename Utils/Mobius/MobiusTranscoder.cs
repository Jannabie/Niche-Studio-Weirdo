using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace NicheStudioWeirdo.Utils.Mobius
{
    public static class MobiusTranscoder
    {
        public static void Transcode(string inputPath, string outputPath, string ffmpegPath, Action<string> logCallback)
        {
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException($"FFmpeg not found at {ffmpegPath}");
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Input file not found at {inputPath}");

            int maxQueueSize = 256;
            // Extract to MP4 using LOSSLESS H.264 (CRF 0) — zero quality loss, safe for video editors
            string options = "-c:v libx264 -crf 0 -preset ultrafast -c:a aac -ac 2 -b:a 320k";
            string stereoTarget = "sbs2l";

            var headers = MobiContainer.GetHeaders(inputPath);
            var headerA = headers.OfType<MoflexAudioFrame>().SingleOrDefault();
            var headerV = headers.OfType<VideoFrame>().Single();

            var decoderA = headerA?.GetDecoder();
            var decoderV = new MobiDecoder(headerV.Width, headerV.Height);
            
            // Randomize pipe name to avoid conflicts if run multiple times
            var pipeName = $"mobius{decoderA?.GetType()?.Name}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            var stereoFilter = string.IsNullOrEmpty(headerV.Stereo) ? "" : $"-vf stereo3d={headerV.Stereo}:{stereoTarget}";
            var inputArgsA = headerA is null ? "" :
                $@"-thread_queue_size {maxQueueSize} -guess_layout_max 0 -f s16le -ar {headerA.Frequency} -ac {headerA.Channels} -i \\.\pipe\{pipeName}";
            var inputArgsV = $@"-thread_queue_size {maxQueueSize} -f rawvideo -pix_fmt yuv420p -r {headerV.Fps} -s {headerV.Width}x{headerV.Height} -i -";
            // Strip -ac from options since FFV1 handles audio via pcm_s16le above
            var inputArgsO = $@"-y -hide_banner {stereoFilter} {options} ""{outputPath}""";

            var startInfo = new ProcessStartInfo
            {
                Arguments = $"{inputArgsA} {inputArgsV} {inputArgsO}",
                CreateNoWindow = true,
                FileName = ffmpegPath,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) logCallback(e.Data); };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) logCallback(e.Data); };
                
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                var pipeA = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 0, 0);
                var pipeV = process.StandardInput.BaseStream;
                if (headerA != null) pipeA.WaitForConnection();
                var bcA = new BlockingCollection<byte[]>(maxQueueSize);
                var bcV = new BlockingCollection<byte[]>(maxQueueSize);

                void WriteToStream(BlockingCollection<byte[]> bc, Stream stream)
                {
                    foreach (var data in bc.GetConsumingEnumerable())
                        stream.Write(data, 0, data.Length);
                    stream.Flush();
                    stream.Close();
                }
                
                var taskA = Task.Run(() => WriteToStream(bcA, pipeA));
                var taskV = Task.Run(() => WriteToStream(bcV, pipeV));

                foreach (var frame in MobiContainer.Demux(inputPath))
                {
                    if (frame is VideoFrame video)
                        bcV.Add(decoderV.Decode(video.Stream));
                    else if (frame is MoflexAudioFrame audio)
                        bcA.Add(decoderA.Decode(audio.Stream.ToArray()));
                }

                bcA.CompleteAdding();
                bcV.CompleteAdding();
                
                Task.WaitAll(taskA, taskV);
                process.WaitForExit();
            }
        }
    }
}
