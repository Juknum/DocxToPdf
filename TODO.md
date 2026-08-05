# 📋 DOCX to PDF Conversion To-Do List (OpenXML + PDFsharp)

This document tracks the phased implementation plan for replacing `FreeSpire.Doc` with `DocumentFormat.OpenXml` and `PDFsharp` (MIT License).

---

### Phase 1: Dependency Cleanup & Setup
- [x] **1.1 Remove FreeSpire.Doc**: Remove `FreeSpire.Doc` NuGet dependency from `DocxToPdf.csproj`.
- [x] **1.2 Add OpenXML SDK**: Install `DocumentFormat.OpenXml` (v3.0+).
- [x] **1.3 Configure Cross-Platform Font Resolver**: Implement `PdfSharp.Fonts.IFontResolver` in C# to load system TrueType/OpenType fonts (`Arial`, `Calibri`, `Times New Roman`, etc.) across macOS (`/System/Library/Fonts`), Linux (`/usr/share/fonts`), and Windows.

---

### Phase 2: OpenXML Parser & DOM Data Extraction
- [x] **2.1 Section & Page Geometry Setup**:
  - Extract `<w:pgSz>`: Page width/height and orientation (Portrait vs. Landscape).
  - Extract `<w:pgMar>`: Margins (Top, Bottom, Left, Right, Header, Footer).
  - Convert OpenXML Twips to PDF points (`1 pt = 20 twips`).
- [x] **2.2 Cascading Style Resolution Engine (`styles.xml`)**:
  - Implement style hierarchy: `DocDefaults` $\rightarrow$ `TableStyle` $\rightarrow$ `ParagraphStyle` $\rightarrow$ `CharacterStyle` $\rightarrow$ Direct Paragraph Formatting (`w:pPr`) $\rightarrow$ Direct Run Formatting (`w:rPr`).
- [x] **2.3 Paragraph & Typography Parser**:
  - Extract Paragraph Alignment (`w:jc`: Left, Center, Right, Justified).
  - Extract Spacing Before/After (`w:spacing w:before`, `w:after`) and Line Height (`w:line`, `w:lineRule`).
  - Extract Paragraph Indents (`w:ind w:left`, `w:right`, `w:firstLine`, `w:hanging`).
  - Extract Text Run (`w:r`) properties: Font Family (`w:rFonts`), Font Size (`w:sz`), Bold (`w:b`), Italic (`w:i`), Underline (`w:u`), Color (`w:color`), Background Shading (`w:shd`).
- [x] **2.4 List & Bullet Parser (`numbering.xml`)**:
  - Resolve `<w:numPr>` (`numId` and `ilvl`).
  - Map abstract numbering formats: Bullet symbols (`•`), ordered decimal (`1.`), Roman numerals (`I.`), and multi-level lists with hanging indents.
- [x] **2.5 Image & Drawing Extraction (`w:drawing`)**:
  - Read `<a:blip r:embed="rIdX">` and retrieve image streams from OpenXML part relationships.
  - Parse layout size from `<wp:extent cx="..." cy="...">` (convert EMUs to points: `1 pt = 12,700 EMUs`).
  - Support both inline images (`wp:inline`) and floating/anchored images (`wp:anchor`).
- [x] **2.6 Table Parser (`w:tbl`)**:
  - Extract grid column widths (`<w:tblGrid>`).
  - Parse row heights (`<w:trHeight>`) and repeat header row setting (`<w:tblHeader>`).
  - Handle column spanning (`<w:gridSpan>`) and vertical cell merging (`<w:vMerge>`).
  - Extract cell borders (`<w:tcBorders>`), cell padding (`<w:tblCellMar>`), and cell background fill (`<w:shd>`).
- [x] **2.7 Header, Footer & Page Number Parser**:
  - Read header/footer relationships (`w:headerReference`, `w:footerReference`).
  - Parse dynamic fields (e.g. `PAGE` and `NUMPAGES`).

---

### Phase 3: Layout Engine & PDF Rendering
- [x] **3.1 Line Wrapping & Text Measurement**:
  - Measure text run dimensions using `XGraphics.MeasureString`.
  - Calculate word-wrapping within page margins and table cell bounds.
- [x] **3.2 Flow Layout & Page Break Controller**:
  - Maintain vertical cursor (`YPosition`).
  - Automatically create new PDF pages (`pdf.AddPage()`) when content exceeds `PageHeight - BottomMargin`.
  - Render persistent headers and footers per page.
- [x] **3.3 Table Rendering Engine**:
  - Calculate cell heights dynamically based on wrapped text contents.
  - Render cell background fills, grid line borders, and cell text alignment.
- [x] **3.4 Graphics & Image Renderer**:
  - Draw scaled images onto `XGraphics` context (`gfx.DrawImage`).

---

### Phase 4: Refactoring `Converter.cs` & Verification
- [x] **4.1 Remove Evaluation Page Splitting Hack**:
  - Delete `MAX_PAGES_PER_PART_ALLOWED = 3` and temporary file merging logic required by FreeSpire.Doc.
- [x] **4.2 End-to-End Visual Verification**:
  - Test simple text documents, multi-page documents, tables, lists, and image layouts.
  - Compare generated PDFs with Word native PDF output for pixel accuracy.
