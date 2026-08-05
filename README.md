# DocxToPdf

A .NET tool and library to convert Word `.docx` documents to `.pdf` files, supporting multi-page splitting and merging using **FreeSpire.Doc** and **PDFsharp**.

## Features

- **DOCX to PDF Conversion**: Convert `.docx` files to PDF.
- **Large Document Support**: Automatically splits documents with more than 3 pages into smaller temporary parts during conversion and merges them into a single output PDF.
- **Cross-Platform & Multi-Targeted**: Compatible across macOS, Linux, and Windows, supporting:
	- **.NET Standard 2.0** (compatible with .NET Core 2.0+, .NET 5/6/7/8/9/10, .NET Framework 4.6.1+, Mono, Unity, Xamarin)
	- **.NET 8.0 (LTS)**
	- **.NET 10.0**

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (.NET 8.0 LTS or later recommended).

## Project Structure

- `DocxToPdf/`: Core class library containing `Converter.cs`.
- `ConsoleApp/`: CLI application for converting files.

## How to Build

From the root directory of the repository:

```bash
dotnet build
```

## Usage

### Command Line Interface (CLI)

You can convert a `.docx` file by running the `ConsoleApp` project:

```bash
dotnet run --project ConsoleApp/ConsoleApp.csproj -- input.docx output.pdf
```

If the output path is omitted, it defaults to `output.pdf`:

```bash
dotnet run --project ConsoleApp/ConsoleApp.csproj -- document.docx
```

### Library Usage in C#

You can also use the `DocxToPdf.Converter` in your own C# code:

```csharp
using DocxToPdf;

// Convert a Word file to PDF
Converter.Convert("path/to/document.docx", "path/to/output.pdf");
```

## How It Works

1. Loads the `.docx` document using `Spire.Doc.Document`.
2. Checks the page count of the document:
	 - If **3 pages or fewer**: Directly saves the document as a PDF.
	 - If **more than 3 pages**: Splits the document into 3-page chunks, converts each chunk to a temporary PDF, and merges them using `PdfSharp`.
