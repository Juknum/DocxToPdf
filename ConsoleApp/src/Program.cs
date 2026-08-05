using DocxToPdf;

string inputPath = args.Length > 0 ? args[0] : "sample.docx";
string outputPath = args.Length > 1 ? args[1] : "output.pdf";

if (File.Exists(inputPath)) {
	Converter.Convert(inputPath, outputPath);
} else {
	Console.WriteLine("Usage: dotnet run --project ConsoleApp [input.docx] [output.pdf]");
	Console.WriteLine($"Input file '{inputPath}' does not exist.");
}

