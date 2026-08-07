---
name: csharp-coding-guidelines
description: Guidelines and patterns for C# project directory structure, unit vs E2E test organization, and multi-framework backward compatibility (.NET 9+, .NET 8+, netstandard2.0).
---

# C# Coding Guidelines & Best Practices

This skill defines standard project layout patterns and backward-compatibility guidelines for C# solutions in this repository.

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

## 2. Framework Version Retrocompatibility Guide

When multi-targeting libraries across multiple .NET versions (e.g. `<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>`), always prefer modern, high-performance C# / .NET APIs on supported runtimes while providing clean fallback implementations for legacy runtimes using preprocessor directives.

### Standard Framework Preprocessor Symbols

Use the built-in SDK framework symbols for version checks:

| Preprocessor Symbol | Target Runtimes Included |
| :--- | :--- |
| `#if NET9_0_OR_GREATER` | .NET 9.0, .NET 10.0, and future releases |
| `#if NET8_0_OR_GREATER` | .NET 8.0, .NET 9.0, .NET 10.0, etc. |
| `#if NET6_0_OR_GREATER` | .NET 6.0, .NET 7.0, .NET 8.0, .NET 9.0, .NET 10.0, etc. |
| `#if NETSTANDARD2_0` | Explicitly targets .NET Standard 2.0 |

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

## 3. Best Practices Checklist

1. **Keep Sources in `src/`**: Never place `.cs` source files directly in the project root directory alongside `.csproj`.
2. **Categorize Tests**: Keep Unit tests (`src/Unit/`) and E2E tests (`src/E2E/`) separated in test projects.
3. **Localize Directives**: Keep preprocessor blocks as narrow and localized as possible. Do not duplicate entire class definitions or long business methods.
4. **Multi-Target Verification**: Always test builds against all target frameworks specified in `.csproj` (`dotnet build`).
5. **No Unnecessary Polyfills**: Prefer native preprocessor checks over third-party polyfill packages unless a polyfill is strictly required across the entire codebase.
