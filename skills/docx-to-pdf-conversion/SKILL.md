---
name: docx-to-pdf-conversion
description: Comprehensive guide and reference patterns for building a 1-to-1 DOCX to PDF converter using DocumentFormat.OpenXml and PDFsharp in .NET.
---

# 1-to-1 DOCX to PDF Conversion Guide (.NET & OpenXML)

This skill provides technical specifications, OpenXML element mapping rules, unit conversions, and code patterns for converting Word (`.docx`) files to PDF (`.pdf`) using `DocumentFormat.OpenXml` (Microsoft OpenXML SDK) and `PDFsharp` (MIT License).

> [!NOTE]  
> For the step-by-step project implementation phases and task checklist, refer to [TODO.md](../../TODO.md).

---

## Architecture Overview

Converting a `.docx` file to PDF without third-party proprietary engines requires a 4-layer architecture:

```
┌────────────────────────────────────────┐
│            1. OpenXML Parser           │
│   (DocumentFormat.OpenXml DOM & Rels)  │
└───────────────────┬────────────────────┘
                    │
┌───────────────────▼────────────────────┐
│          2. Style Resolver             │
│ (DocDefaults -> Styles -> Direct pPr)  │
└───────────────────┬────────────────────┘
                    │
┌───────────────────▼────────────────────┐
│          3. Layout Engine              │
│ (Font metrics, Line wrapping, Tables)  │
└───────────────────┬────────────────────┘
                    │
┌───────────────────▼────────────────────┐
│          4. PDF Renderer               │
│     (PDFsharp XGraphics & XDocument)   │
└────────────────────────────────────────┘
```

---

## 1. Unit & Measurement Conversions

Word OpenXML uses multiple unit systems that must be converted to PDF Points (`1 pt = 1/72 inch`):

| Unit in OpenXML | OpenXML Element / Attribute | Conversion Formula to PDF Points |
| :--- | :--- | :--- |
| **Twips** (1/20 of a pt) | Margins (`w:pgMar`), Indents (`w:ind`), Spacing (`w:spacing`), Column widths (`w:gridCol`) | `pt = twips / 20.0` |
| **Half-Points** (1/2 pt) | Font Size (`w:sz`, `w:szCs`) | `pt = halfPt / 2.0` |
| **EMUs** (English Metric Units) | Image dimensions (`cx`, `cy` in `wp:extent`) | `pt = emu / 12700.0` (1 inch = 914,400 EMUs) |
| **Eighths of a Point** | Border widths (`w:sz` in `w:top`, `w:bottom`, etc.) | `pt = eighths / 8.0` |
| **Hex Colors** | `w:color w:val="FF0000"` | Map `"auto"` to Black, otherwise parse RGB hex string |

---

## 2. Style Resolution Cascade

To obtain 1-to-1 typography and spacing, run properties (`rPr`) and paragraph properties (`pPr`) must be evaluated in strict priority order:

1. **Document Defaults**: Found in `styles.xml` under `<w:docDefaults>`.
2. **Table Style**: Default cell/row styles if element is inside a table.
3. **Paragraph Style**: Look up `w:pStyle` in `styles.xml` (follow `w:basedOn` inheritance tree).
4. **Character Style**: Look up `w:rStyle` in `styles.xml` (follow `w:basedOn` inheritance tree).
5. **Direct Paragraph Formatting**: Local `<w:pPr>` on `<w:p>`.
6. **Direct Run Formatting**: Local `<w:rPr>` on `<w:r>`.

---

## 3. Core OpenXML Extraction Specifications

### Section & Page Setup (`w:sectPr`)
- **Page Size**: `<w:pgSz w:w="12240" w:h="15840" w:orient="portrait"/>`
  - `Width = 12240 / 20 = 612 pt` (8.5 inches)
  - `Height = 15840 / 20 = 792 pt` (11.0 inches)
- **Margins**: `<w:pgMar w:top="1440" w:bottom="1440" w:left="1440" w:right="1440"/>`
  - `Margins = 1 inch (72 pt)` on all sides.

### Paragraph Spacing & Alignment (`w:pPr`)
- **Alignment** (`w:jc`): `left`, `center`, `right`, `both` (justified).
- **Space Before / After** (`w:spacing w:before="..." w:after="..."`): Convert twips to pt.
- **Line Spacing** (`w:spacing w:line="..." w:lineRule="..."`):
  - `auto`: line height is a multiple of font size (e.g. 240 twips = 1.0x, 360 twips = 1.5x).
  - `exact` / `atLeast`: converted from twips to pt.
- **Indents** (`w:ind`): `w:left`, `w:right`, `w:firstLine`, `w:hanging`.

### Text Runs (`w:r`)
- Text (`w:t`), Tab (`w:tab`), Soft Break (`w:br`), Hard Page Break (`w:br w:type="page"`).
- Formatting (`w:rPr`):
  - `w:rFonts`: `w:ascii`, `w:hAnsi`, `w:cs`.
  - `w:sz`: Font size in half-points.
  - `w:b`: Bold flag.
  - `w:i`: Italic flag.
  - `w:u`: Underline style (`single`, `double`, etc.).
  - `w:strike`: Strikethrough flag.
  - `w:color`: Text color (hex RGB).
  - `w:shd` / `w:highlight`: Text background color.

### Numbering & Bullet Lists (`numbering.xml`)
- Check `w:numPr`: `<w:numId w:val="X"/>` and `<w:ilvl w:val="Y"/>`.
- Find `<w:num w:numId="X">` -> maps to `<w:abstractNum w:abstractNumId="Z">`.
- Lookup level `<w:lvl w:ilvl="Y">`:
  - `w:lvlText`: Format string (e.g., `"%1."`, `"%1.%2."`, `"•"`).
  - `w:numFmt`: `decimal`, `lowerLetter`, `upperRoman`, `bullet`, etc.
  - `w:pPr / w:ind`: Bullet position and text hanging indent.

### Tables (`w:tbl`)
- **Column Widths**: Read `<w:tblGrid>` -> `<w:gridCol w:w="...">`.
- **Row Heights**: Read `<w:trPr>` -> `<w:trHeight w:val="..." w:hRule="exact|atLeast"/>`.
- **Header Repeat**: `<w:tblHeader/>` indicates the row repeats at top of new pages.
- **Cell Merging**:
  - Horizontal: `<w:gridSpan w:val="N"/>` spans N columns.
  - Vertical: `<w:vMerge w:val="restart"/>` begins a merged cell, `<w:vMerge/>` continues it.
- **Borders & Shading**: Read `<w:tcBorders>` and `<w:shd w:fill="HEX"/>`.

### Images & Drawings (`w:drawing`)
- Extract `blip` element: `<a:blip r:embed="rIdX"/>`.
- Map `rIdX` in `MainDocumentPart.GetPartById(rId)` to retrieve image stream.
- Parse width & height from `<wp:extent cx="EMU_W" cy="EMU_H"/>`.
- Determine positioning: `<wp:inline>` (flows with text) vs `<wp:anchor>` (floating relative to margin/page/paragraph).

---

## 4. PDFsharp Layout & Rendering Patterns

### Font Resolving on Cross-Platform (macOS / Linux / Windows)
PDFsharp requires a custom `IFontResolver` implementation to load system fonts (e.g. Arial, Times New Roman, Liberation Sans):

```csharp
using PdfSharp.Fonts;

public class CustomFontResolver : IFontResolver {
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) {
        string name = familyName.ToLowerInvariant();
        string style = (isBold ? "b" : "") + (isItalic ? "i" : "");
        return new FontResolverInfo($"{name}#{style}");
    }

    public byte[] GetFont(string faceName) {
        // Read font file from system directory (/System/Library/Fonts, /usr/share/fonts, or C:\Windows\Fonts)
        string path = ResolveFontPath(faceName);
        return File.ReadAllBytes(path);
    }
}
```

### Page & Cursor Management
```csharp
using PdfSharp.Pdf;
using PdfSharp.Drawing;

PdfDocument pdf = new PdfDocument();
PdfPage page = pdf.AddPage();
page.Width = XUnit.FromPoint(612); // Letter width
page.Height = XUnit.FromPoint(792); // Letter height
XGraphics gfx = XGraphics.FromPdfPage(page);

double currentY = topMargin;
double leftMargin = 72; // 1 inch
double rightMargin = 612 - 72;
double printableWidth = rightMargin - leftMargin;
```

---

## 5. Verification Checklist for 1-to-1 Fidelity

- [ ] **Page Bounds & Margins**: Verify page orientation and margins match original DOCX.
- [ ] **Font Matching & Fallback**: Ensure embedded or system fonts match specified styles.
- [ ] **Line Wrapping Accuracy**: Verify string width calculation (`XGraphics.MeasureString`) matches Word text layout.
- [ ] **Paragraph Alignment & Spacing**: Confirm left/center/right/justified alignment and spacing before/after.
- [ ] **List Numbering & Bullets**: Verify correct list prefixes and hierarchical indentations.
- [ ] **Table Spans & Borders**: Verify cell merging (`gridSpan`/`vMerge`), padding, and borders render cleanly.
- [ ] **Image Placement**: Confirm images scale accurately according to EMU measurements without stretching.
- [ ] **Header / Footer Consistency**: Ensure page numbers and header/footer text render on designated pages.
