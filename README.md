# DocxToPdf

A cross-platform .NET library and CLI tool to convert Word `.docx` documents to high-fidelity `.pdf` files using **DocumentFormat.OpenXml** and **PDFsharp**.

## Features

- **DOCX to PDF Conversion**: Native OpenXML parsing and layout rendering for `.docx` documents.
- **Cross-Platform**: Operates seamlessly across macOS, Linux, and Windows with custom cross-platform font resolution.
- **End-to-End Visual Verification**: Built-in test suite and GitHub Actions workflow comparing page images at 90%+ pixel similarity.
- **Multi-Targeted**: Supports `.NET Standard 2.0`, `.NET 8.0 (LTS)`, and `.NET 10.0`.

## Project Structure

- `DocxToPdf/`: Core conversion engine library (`Converter.cs`).
- `DocxToPdf.Tests/`: Unit tests and `E2EWorkflowTests.cs` for automated visual page comparison.
- `DocxToPdf.Files/`: E2E sample datasets following the standard test architecture:
  - `<FileName>/input.docx`: Input Word document.
  - `<FileName>/expected.pdf`: Ground-truth expected PDF.
  - `<FileName>/output.pdf`: Generated test output PDF (gitignored).
- `ConsoleApp/`: CLI application for manual conversion and E2E verification.

## How to Build

From the root directory of the repository:

```bash
dotnet build
```

## Usage

### Command Line Interface (CLI)

Convert a `.docx` file using `ConsoleApp`:

```bash
dotnet run --project ConsoleApp -- input.docx output.pdf
```

Run E2E visual verification on all samples in `DocxToPdf.Files`:

```bash
dotnet run --project ConsoleApp -f net10.0 -- verify
```

### Running E2E Test Verification Suite

Run all tests including E2E visual page comparison:

```bash
dotnet test
```

### Library Usage in C#

Use `DocxToPdf.Converter` in your C# projects:

```csharp
using DocxToPdf;

// Convert a Word file to PDF
Converter.Convert("path/to/document.docx", "path/to/output.pdf");
```

## E2E Architecture & Git Rules

All test samples under `DocxToPdf.Files/<FileName>/` adhere to:
- `input.docx` (tracked)
- `expected.pdf` (tracked)
- `output.pdf` (gitignored)
- Extracted and diff page images `*.png` (gitignored)

Images are compared page-by-page against a **90% pixel match threshold**.
