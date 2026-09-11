# EditorConfigLanguage.Benchmarks

An isolated [BenchmarkDotNet](https://benchmarkdotnet.org/) (0.15.8) project that measures five
performance-sensitive scenarios in `src\EditorConfig.csproj`:

1. **Typing / debounced parsing** (`TypingDebounceBenchmarks`) - simulates rapid keystrokes and
   measures the real 150ms-debounced reparse pipeline in `EditorConfigDocumentParser`.
2. **Large-file parsing** (`LargeFileParsingBenchmarks`) - parses synthetic `.editorconfig`
   documents with 200/1000/5000 sections.
3. **Editorconfig hierarchy lookup** (`HierarchyLookupBenchmarks`) - walks the real
   `EditorConfigDocument.Parent` ancestor-directory search at increasing depth.
4. **Document memory retention after disposal** (`DocumentDisposalBenchmarks`) - compares
   allocations/retention between disposing and not disposing a document.
5. **Pathological glob matching** (`GlobMatchingBenchmarks`) - compiles and matches adversarial
   section-name patterns (wide alternation, deep nesting, wildcard + numeric ranges) against the
   real `AnalyzerConfig` section-name matcher.

All five exercise the genuine production parsing/matching/inheritance code in `EditorConfig.dll`
(via a `ProjectReference` to `src\EditorConfig.csproj`) - nothing here is re-implemented or
mocked out. `[MemoryDiagnoser]` is enabled on every benchmark class.

## Running

```powershell
dotnet run -c Release --project Benchmark\EditorConfigLanguage.Benchmarks\EditorConfigLanguage.Benchmarks.csproj
```

BenchmarkDotNet will prompt you to pick which benchmark class(es) to run, or you can target one
directly:

```powershell
dotnet run -c Release --project Benchmark\EditorConfigLanguage.Benchmarks\EditorConfigLanguage.Benchmarks.csproj -- --filter "*GlobMatchingBenchmarks*"
```

A quick, low-fidelity smoke test (single iteration, useful for verifying nothing throws after a
change) can be run with:

```powershell
dotnet run -c Release --project Benchmark\EditorConfigLanguage.Benchmarks\EditorConfigLanguage.Benchmarks.csproj -- --filter "*" --job Dry
```

## Why this project looks the way it does

- **Target framework**: `net48`, matching `src\EditorConfig.csproj` exactly, so the `ProjectReference`
  is binary-compatible.
- **No `InternalsVisibleTo`**: `EditorConfig.dll` only grants `InternalsVisibleTo` to
  `Test\EditorConfigTest` (see `src\Properties\AssemblyInfo.cs`), and per the task constraints this
  project must not modify `src`. `EditorConfigDocument` and `AnalyzerConfig` (the section-name/glob
  matcher) are both `internal` classes, so `Support\ProductionApi.cs` centralizes a handful of
  reflection calls into their **public members** (reflection visibility is checked per-member, not
  per-enclosing-type, so this works without `InternalsVisibleTo`). Every reflected member is
  documented inline with why it's needed and why no public alternative exists.
- **No live Visual Studio/MEF host**: constructing a real `ITextBuffer` normally requires
  `Microsoft.VisualStudio.Platform.VSEditor.dll`, which is not published on NuGet and isn't present
  in this repository. `EditorConfigDocument.CreateForTest(ITextBuffer, string)` - an existing
  `internal static` factory already used throughout `Test\EditorConfigTest` - bypasses the
  VS-UI-thread-only `InitializeInheritance()` path while still running the real parser. Since
  `ITextBuffer`/`ITextSnapshot`/`ITextSnapshotLine`/`IContentType` are plain data contracts with no
  COM/UI-thread dependency, `TestDoubles\` contains small, hand-rolled implementations backing only
  the members the parser actually touches; everything else throws `NotSupportedException` so an
  accidental new dependency fails loudly instead of silently skewing results.
- **Hierarchy lookup uses a synthetic, non-existent drive (`X:\...`)**: `EditorConfigDocument.Parent`
  walks ancestor directories checking `File.Exists`, and only touches the (unavailable, since
  `CreateForTest` skips MEF import) `DocumentService` if an ancestor `.editorconfig` is actually
  found. Because the real ancestor of this repository (`.editorconfig` at the repo root) exists,
  hierarchy-lookup benchmarks intentionally use a synthetic path under a drive letter that doesn't
  exist on this machine, so the walk always safely terminates at the (non-existent) root - this
  exercises the real, unmodified directory-walking logic while avoiding the one code path
  (`InheritsFrom()` recursively constructing a parent `EditorConfigDocument` with
  `initializeInheritance: true`) that would require a real VS UI thread.
- **`TypingDebounceBenchmarks` may print `// Exceptions: N`**: this is expected. Every keystroke but
  the last cancels its predecessor's pending (debounced) parse, which throws and swallows an
  `OperationCanceledException` inside `EditorConfigDocumentParser.ParseAsync`. It's a direct,
  benign measurement of the debounce mechanism working as designed, not a bug in the benchmark.

## What this project owns

Only this folder (`Benchmark\`) and the single new `<Project>` entry it adds to `EditorConfig.slnx`.
No files under `src\` or `Test\` are modified.
