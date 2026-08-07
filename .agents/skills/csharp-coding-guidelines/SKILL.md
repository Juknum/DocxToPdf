---
name: csharp-coding-guidelines
description: Guidelines and patterns for C# project directory structure, unit vs E2E test organization, PolySharp retrocompatibility, early returns, modern syntax, XML documentation, and multi-framework backward compatibility (.NET 9+, .NET 8+, netstandard2.0).
---

# C# Coding Guidelines & Best Practices

This skill defines standard project layout patterns, modern C# writing standards, XML documentation standards, and backward-compatibility guidelines for C# solutions in this repository.

---

## 1. Project Directory Structure Standard

Each C# project (`.csproj`) in the repository MUST follow a clean three-tier folder structure:

```
ProjectName/
├── ProjectName.csproj
├── bin/                       # Generated build binaries (ignored in git)
├── obj/                       # Generated intermediate build outputs (ignored in git)
└── src/                       # All C# source code (.cs)
```

### Test Project Organization (`DocxToPdf.Tests`)
Test projects under `src/` MUST separate test categories into dedicated subdirectories:

```
DocxToPdf.Tests/
├── DocxToPdf.Tests.csproj
├── bin/
├── obj/
└── src/
    ├── Unit/                  # Unit test classes (isolated component testing)
    │   ├── ConverterTests.cs
    │   ├── DocxParserTests.cs
    │   ├── FontResolverTests.cs
    │   └── ...
    └── E2E/                   # End-to-end integration test classes
        └── E2EWorkflowTests.cs
```

---

## 2. Coding Style & Language Standards

### A. Always Prefer Early Returns (Guard Clauses)
Avoid deep nesting of conditional `if-else` blocks. Fail fast, validate inputs at the start of methods, and return early to keep the main execution path clean and un-indented.

```csharp
// BAD: Deeply nested control flow
public ProcessingResult ProcessDocument(Document? doc, Options? options) {
	if (doc != null) {
		if (options != null) {
			if (doc.IsValid) {
				// Business logic here...
				return ProcessingResult.Success;
			} else {
				return ProcessingResult.InvalidDocument;
			}
		} else {
			throw new ArgumentNullException(nameof(options));
		}
	} else {
		throw new ArgumentNullException(nameof(doc));
	}
}

// GOOD: Early returns with guard clauses
public ProcessingResult ProcessDocument(Document? doc, Options? options) {
	ArgumentNullException.ThrowIfNull(doc);
	ArgumentNullException.ThrowIfNull(options);

	if (!doc.IsValid) {
		return ProcessingResult.InvalidDocument;
	}

	// Main execution path cleanly at root indentation
	// ...
	return ProcessingResult.Success;
}
```

---

### B. Use Modern C# Language Features
Always target `<LangVersion>latest</LangVersion>` in `.csproj` and leverage modern C# language features to keep code concise, expressive, and type-safe:

* **File-scoped namespaces**: `namespace DocxToPdf.Src;` (saves horizontal indentation)
* **Primary constructors**: `public class DocumentConverter(ILogger logger, IFontResolver fontResolver)`
* **Pattern matching & switch expressions**: Use `is not null`, `is string s`, and expression switches (`x switch { ... }`)
* **Collection expressions**: `int[] numbers = [1, 2, 3];` or `List<string> items = [];`
* **Target-typed `new()`**: `List<Element> list = new();`
* **Expression-bodied members**: `public string Id => _id;`
* **Raw string literals**: `""" {"name": "test"} """`

---

### C. XML Documentation Requirements
Every public and internal symbol (classes, interfaces, structs, enums, properties, methods, fields, events) MUST be documented with complete C# XML documentation comments (`///`).

* Include `<summary>`, `<param>`, `<returns>`, and `<exception>` tags where applicable.

#### Example XML Documentation:
```csharp
/// <summary>
/// Converts a DOCX document stream into a rendered PDF document.
/// </summary>
/// <param name="docxStream">The input stream containing the DOCX file content. Cannot be null.</param>
/// <param name="options">Optional conversion configuration options.</param>
/// <returns>A <see cref="Stream"/> containing the generated PDF output.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="docxStream"/> is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when parsing the DOCX structure fails.</exception>
public Stream ConvertToPdf(Stream docxStream, ConversionOptions? options = null) {
	ArgumentNullException.ThrowIfNull(docxStream);
	// ...
}
```

---

## 3. Framework Version Retrocompatibility Guide

When multi-targeting libraries across multiple .NET versions (e.g. `<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>`), always prefer modern, high-performance C# / .NET APIs on supported runtimes while providing clean fallback implementations for legacy runtimes using preprocessor directives or PolySharp source generator polyfills.

### Standard Framework Preprocessor Symbols

Use the built-in SDK framework symbols for version checks:

| Preprocessor Symbol | Target Runtimes Included |
| :--- | :--- |
| `#if NET9_0_OR_GREATER` | .NET 9.0, .NET 10.0, and future releases |
| `#if NET8_0_OR_GREATER` | .NET 8.0, .NET 9.0, .NET 10.0, etc. |
| `#if NET6_0_OR_GREATER` | .NET 6.0, .NET 7.0, .NET 8.0, .NET 9.0, .NET 10.0, etc. |
| `#if NETSTANDARD2_0` | Explicitly targets .NET Standard 2.0 |

---

### PolySharp NuGet for C# Language Feature Retrocompatibility

Use the **[PolySharp](https://github.com/Sergio0694/PolySharp)** NuGet package to enable modern C# language features when targeting older runtimes (such as `netstandard2.0` or `.NET Framework`) without introducing runtime dependencies or manual polyfill shims.

#### 1. Package Installation & Setup
Add `PolySharp` as a development-only dependency in your `.csproj` or `Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="PolySharp" Version="1.15.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

> [!NOTE]
> `PolySharp` operates exclusively as a compile-time Roslyn source generator. Setting `PrivateAssets="all"` guarantees that polyfill types remain internal to the assembly and are never leaked transitively to downstream consumers.

#### 2. Features Polyfilled Automatically by PolySharp
When `PolySharp` is referenced, the compiler allows modern syntax on legacy framework targets:
* **Init-only Properties**: `public string Name { get; init; }` (`IsExternalInit`)
* **Required Members**: `public required string Id { get; init; }` (`RequiredMemberAttribute`, `SetsRequiredMembersAttribute`)
* **Record & Record Struct Types**: `public record Person(string Name);`
* **Index and Range Expressions**: `span[1..^1]`, `array[^1]` (`System.Index`, `System.Range`)
* **Nullable Reference Type Attributes**: `[NotNullWhen]`, `[MaybeNullWhen]`, `[MemberNotNull]`, `[NotNullIfNotNull]`
* **Caller Argument Expressions**: `[CallerArgumentExpression("param")]`
* **Interpolated String Handlers**: `[InterpolatedStringHandler]`
* **String Syntax & Modern Code Analysis Attributes**: `[StringSyntax]`, `[Unreachable]`

#### 3. PolySharp vs. Preprocessor Directives (`#if`)
* **ALWAYS Prefer PolySharp First**: Always prefer PolySharp-provided polyfills and language features over `#if` preprocessor directives. PolySharp allows writing uniform, modern syntax across all target frameworks without cluttering code with conditional compilation blocks.
* **Use Preprocessor Directives (`#if`) Only as a Last Resort**: Reserve `#if` / `#else` / `#endif` blocks strictly for runtime BCL API differences that cannot be handled by PolySharp (such as `System.Threading.Lock`, `FrozenDictionary`, `SearchValues<T>`, or `TimeProvider`).

---

### Common Modern vs. Fallback Code Patterns

#### A. Synchronization & Locking (`System.Threading.Lock`)
*Introduced in .NET 9 / C# 13.*
```csharp
#if NET9_0_OR_GREATER
	private static readonly System.Threading.Lock SyncLock = new();
#else
	private static readonly object SyncLock = new();
#endif

public void DoWork() {
	lock (SyncLock) {
		// Thread-safe operation
	}
}
```

#### B. High-Performance Immutable Lookups (`FrozenDictionary` / `FrozenSet`)
*Introduced in .NET 8.*
```csharp
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

public class LookupCache {
#if NET8_0_OR_GREATER
	private static readonly FrozenDictionary<string, string> Cache = InitialData.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
#else
	private static readonly Dictionary<string, string> Cache = new(InitialData, StringComparer.OrdinalIgnoreCase);
#endif
}
```

#### C. Fast Pattern Matching & Searching (`SearchValues<T>`)
*Introduced in .NET 8.*
```csharp
#if NET8_0_OR_GREATER
using System.Buffers;

private static readonly SearchValues<char> DisallowedChars = SearchValues.Create("<>:\"/\\|?*");

public bool HasInvalidChars(ReadOnlySpan<char> input) {
	return input.ContainsAny(DisallowedChars);
}
#else
private static readonly char[] DisallowedChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

public bool HasInvalidChars(string input) {
	return input.IndexOfAny(DisallowedChars) >= 0;
}
#endif
```

#### D. System Time Abstraction (`TimeProvider`)
*Introduced in .NET 8.*
```csharp
#if NET8_0_OR_GREATER
	private readonly TimeProvider _timeProvider = TimeProvider.System;
#else
	// Custom interface or DateTime.UtcNow fallback for older runtimes
#endif
```

---

## 4. Best Practices Checklist

1. **Keep Sources in `src/`**: Never place `.cs` source files directly in the project root directory alongside `.csproj`.
2. **Categorize Tests**: Keep Unit tests (`src/Unit/`) and E2E tests (`src/E2E/`) separated in test projects.
3. **Prefer Early Returns**: Structure logic with guard clauses to avoid deeply nested conditional branches.
4. **Use Modern C# Syntax**: Apply modern language features (`LangVersion=latest`) like primary constructors, file-scoped namespaces, pattern matching, and collection expressions.
5. **Mandatory XML Documentation**: Document every class, interface, enum, method, and property with complete `///` XML comments and ensure `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is enabled in `.csproj`.
6. **Prefer PolySharp over `#if` Directives**: ALWAYS prefer PolySharp features over `#if` directives. Use `#if` conditional compilation only when PolySharp cannot bridge a runtime BCL API difference.
7. **Localize Directives**: Keep preprocessor blocks as narrow and localized as possible when runtime BCL differences force their use. Do not duplicate entire class definitions or long business methods.
8. **Multi-Target Verification**: Always test builds against all target frameworks specified in `.csproj` (`dotnet build`).



