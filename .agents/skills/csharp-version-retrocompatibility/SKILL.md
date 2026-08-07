---
name: csharp-version-retrocompatibility
description: Guidelines and patterns for adopting modern C# / .NET features (.NET 9+, .NET 8+) via preprocessor directives while preserving backward compatibility with older target frameworks like netstandard2.0 and net8.0.
---

# C# Modern Feature Adoption & Retrocompatibility Guide

When multi-targeting libraries across multiple .NET versions (e.g. `<TargetFrameworks>netstandard2.0;net8.0;net10.0</TargetFrameworks>`), always prefer modern, high-performance C# / .NET APIs on supported runtimes while providing clean fallback implementations for legacy runtimes using preprocessor directives.

---

## 1. Standard Framework Preprocessor Symbols

Use the built-in SDK framework symbols for version checks:

| Preprocessor Symbol | Target Runtimes Included |
| :--- | :--- |
| `#if NET9_0_OR_GREATER` | .NET 9.0, .NET 10.0, and future releases |
| `#if NET8_0_OR_GREATER` | .NET 8.0, .NET 9.0, .NET 10.0, etc. |
| `#if NET6_0_OR_GREATER` | .NET 6.0, .NET 7.0, .NET 8.0, .NET 9.0, .NET 10.0, etc. |
| `#if NETSTANDARD2_0` | Explicitly targets .NET Standard 2.0 |

---

## 2. Common Modern vs. Fallback Code Patterns

### A. Synchronization & Locking (`System.Threading.Lock`)
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

### B. High-Performance Immutable Lookups (`FrozenDictionary` / `FrozenSet`)
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

### C. Fast Pattern Matching & Searching (`SearchValues<T>`)
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

### D. System Time Abstraction (`TimeProvider`)
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

1. **Localize Directives**: Keep preprocessor blocks as narrow and localized as possible (e.g. on field declarations or helper methods). Do not duplicate entire class definitions or long business methods.
2. **Multi-Target Verification**: Always test builds against all target frameworks specified in `.csproj` (`dotnet build`).
3. **No Unnecessary Polyfills**: Prefer native preprocessor checks over third-party polyfill packages unless a polyfill is strictly required across the entire codebase.
