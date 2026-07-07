using System;
using System.IO;
using System.Text;

namespace Malie_Script_Tool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Malie Script Tool");
                Console.WriteLine("  created by Crsky");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  Disassemble    : Malie_Script_Tool -d -in [input.dat] -out [output.txt]");
                Console.WriteLine("  Export Strings : Malie_Script_Tool -a -in [input.dat] -out [output.txt]");
                Console.WriteLine("  Import Strings : Malie_Script_Tool -s -in [input.dat] -out [output.dat] -txt [input.txt]");
                Console.WriteLine("  Export Text    : Malie_Script_Tool -e -in [input.dat] -out [output.txt]");
                Console.WriteLine("  Import Text    : Malie_Script_Tool -i -in [input.dat] -out [output.dat] -txt [input.txt]");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");

                Environment.ExitCode = 1;
                Console.ReadKey();

                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parsedArgs = CommandLineParser.ParseArguments(args);

            // Common arguments
            CommandLineParser.EnsureArguments(parsedArgs, "-in", "-out");

            var inputPath = Path.GetFullPath(parsedArgs["-in"]);
            var outputPath = Path.GetFullPath(parsedArgs["-out"]);

            // Disassemble
            if (parsedArgs.ContainsKey("-d"))
            {
                var script = new Script();
                script.Load(inputPath);
                script.ExportDisasm(outputPath);
                return;
            }

            // Export All Strings
            if (parsedArgs.ContainsKey("-a"))
            {
                var script = new Script();
                script.Load(inputPath);
                script.ExportStrings(outputPath);
                return;
            }

            // Export Text
            if (parsedArgs.ContainsKey("-e"))
            {
                var script = new Script();
                script.Load(inputPath);
                script.ExportMessages(outputPath);
                return;
            }

            // Import Strings
            if (parsedArgs.ContainsKey("-s"))
            {
                CommandLineParser.EnsureArguments(parsedArgs, "-txt");

                var txtPath = Path.GetFullPath(parsedArgs["-txt"]);

                var script = new Script();
                script.Load(inputPath);
                script.ImportStrings(txtPath);
                script.Save(outputPath);

                return;
            }

            // Import Text
            if (parsedArgs.ContainsKey("-i"))
            {
                CommandLineParser.EnsureArguments(parsedArgs, "-txt");

                var txtPath = Path.GetFullPath(parsedArgs["-txt"]);

                var script = new Script();
                script.Load(inputPath);
                script.ImportMessages(txtPath);
                script.Save(outputPath);

                return;
            }
        }
    }
}
