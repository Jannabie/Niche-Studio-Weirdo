using System;
using NicheStudioWeirdo.Utils;

class Program
{
    static void TestMain()
    {
        string inputCsv = @"C:\Users\user\Downloads\Visual Novel\Percobaan Parser\Nyoba\1001.txt";
        string outputTxt = @"C:\Users\user\Downloads\Visual Novel\Percobaan Parser\Nyoba\1001_parsed.txt";
        string outputCsv = @"C:\Users\user\Downloads\Visual Novel\Percobaan Parser\Nyoba\1001_repacked.txt";

        Console.WriteLine("Parsing CSV to TXT...");
        LeafTxtTool.ParseCsvToTxt(inputCsv, outputTxt);
        Console.WriteLine("Done parsing.");
        
        Console.WriteLine("Injecting TXT to CSV...");
        LeafTxtTool.InjectTxtToCsv(outputTxt, outputCsv);
        Console.WriteLine("Done injecting.");
    }
}
