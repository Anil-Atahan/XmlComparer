# XmlComparer

XmlComparer is a zero-dependency .NET library for structural XML comparison with an HTML side-by-side diff report.

## Table of Contents

- [Installation](#installation)
- [Basic Concepts](#basic-concepts)
- [Quickstart (CLI)](#quickstart-cli)
- [Library Usage](#library-usage)
- [Which API Should I Use?](#which-api-should-i-use)
- [Advanced Topics](#advanced-topics)
- [Performance](#performance)
- [Error Handling](#error-handling)
- [JSON Output Schema](#json-output-schema)
- [Projects](#projects)

---

## Installation

### .NET CLI

```bash
dotnet add package XmlComparer.Core
```

### Package Manager Console

```powershell
Install-Package XmlComparer.Core
```

### NuGet Package Manager

Search for `XmlComparer.Core` in the NuGet Package Manager.

### Requirements

- .NET 9.0 or later
- Zero external dependencies

---

## Basic Concepts

### Key Attributes

Key attributes uniquely identify elements among siblings. When specified, the library uses these attributes to match elements even when their order changes.

**Example:**

```xml
<!-- Without key attributes: Elements compared by position -->
<users>
  <user><name>Alice</name></user>
  <user><name>Bob</name></user>
</users>

<!-- With key attribute "id": Elements matched by their id -->
<users>
  <user id="2"><name>Bob</name></user>
  <user id="1"><name>Alice</name></user>
</users>
```

Configure key attributes:

```csharp
options.WithKeyAttributes("id", "code", "sku");
```

### Diff Types

The library detects five types of changes:

| Type | Description | Color in Report |
|------|-------------|-----------------|
| `Unchanged` | No differences detected | Gray |
| `Added` | Element exists only in the new document | Green |
| `Deleted` | Element exists only in the original document | Red |
| `Modified` | Same element with different attributes or content | Blue |
| `Moved` | Same element at a different position | Yellow |

---

## Quickstart (CLI)

Run the runner against two XML files and generate an HTML report:

```powershell
dotnet run --project XmlComparer.Runner -- .\original.xml .\new.xml --out diff.html --json diff.json --key id,code
```

**Options:**

| Option | Description |
|--------|-------------|
| `--out <path>` | Output HTML path (default `diff.html`) |
| `--json [path]` | Output JSON diff (default `diff.json` if flag is present) |
| `--json-only` | Skip HTML output and emit JSON only |
| `--validation-only` | Validate against XSDs and emit validation result only |
| `--key <comma-list>` | Key attributes used to match elements |
| `--ignore-values` | Ignore text values when comparing |
| `--xsd <path>` | Validate both XML files against XSD (repeatable) |

The HTML report embeds the JSON diff in a `<script type="application/json" id="xml-diff-json">` tag for single-file portability. If schema validation is requested, the JSON includes a `Validation` block alongside `Diff`, and the HTML shows a validation status panel.

---

## Library Usage

### Basic File Comparison

```csharp
using XmlComparer.Core;

var diff = XmlComparer.CompareFiles("old.xml", "new.xml");

if (diff.Type != DiffType.Unchanged)
{
    Console.WriteLine($"Documents differ: {diff.Type}");
}
```

### With Configuration

```csharp
using XmlComparer.Core;

var result = XmlComparer.CompareFilesWithReport(
    "old.xml",
    "new.xml",
    options => options
        .WithKeyAttributes("id", "code")
        .ExcludeAttributes("timestamp")
        .ExcludeNodes(excludeSubtree: true, "metadata")
        .NormalizeWhitespace()
        .IncludeHtml()
        .IncludeJson());

File.WriteAllText("diff.html", result.Html!);
File.WriteAllText("diff.json", result.Json!);
```

### Using XmlComparerService

```csharp
using XmlComparer.Core;

var config = new XmlDiffConfig
{
    IgnoreValues = false
};
config.KeyAttributeNames.Add("id");
config.ExcludedNodeNames.Add("metadata");
config.ExcludedAttributeNames.Add("timestamp");

var service = new XmlComparerService(config);
var diff = service.Compare(@"C:\path\old.xml", @"C:\path\new.xml");
string html = service.GenerateHtml(diff);
```

---

## Which API Should I Use?

Choose the API that best fits your scenario:

| Scenario | Recommended API | Notes |
|----------|-----------------|-------|
| Quick scripts | Static `XmlComparer` | Simplest, no setup required |
| Web/ DI apps | `XmlComparerClient` | DI-friendly, reusable config |
| Custom matching logic | Implement `IMatchingStrategy` | Full control over element matching |
| Async operations | `*Async` methods | True async I/O for large files |
| Extension methods | String extensions | Fluent syntax on strings |

---

## Advanced Topics

### Custom Matching Strategies

Implement `IMatchingStrategy` for custom element matching:

```csharp
public class CustomStrategy : IMatchingStrategy
{
    public double Score(XElement? e1, XElement? e2, XmlDiffConfig config)
    {
        if (e1 == null || e2 == null) return 0.0;
        if (e1.Name != e2.Name) return 0.0;

        var name1 = e1.Attribute("name")?.Value;
        var name2 = e2.Attribute("name")?.Value;

        return name1 == name2 && name1 != null ? 100.0 : 0.0;
    }
}

// Use your strategy
var result = XmlComparer.CompareFilesWithReport("a.xml", "b.xml",
    options => options.UseMatchingStrategy(new CustomStrategy()));
```

### Performance Considerations

- **Small files (< 1MB)**: Use synchronous methods for simplicity
- **Large files (> 10MB)**: Use async methods to avoid blocking
- **Memory usage**: Entire documents are loaded into memory
- **HTML generation**: Can be memory-intensive for large diffs

**Limits:**
- Maximum file size: 100MB
- Maximum recursion depth: 1000 levels
- Maximum children per node: 10,000

### Options Persistence

Save and load comparison options:

```csharp
var options = new XmlComparisonOptions()
    .WithKeyAttributes("id")
    .ExcludeAttributes("timestamp")
    .IncludeHtml();

// Save to file
options.SaveToFile("options.json");

// Load from file
var loaded = XmlComparisonOptions.LoadFromFile("options.json");

// JSON serialization
string json = options.ToJson();
var fromJson = XmlComparisonOptions.FromJson(json);
```

---

## Performance

Benchmark results from .NET 9.0 (Release build, x64):

### XML Comparison Performance

| Document Size | Mean Time | Throughput |
|--------------|-----------|------------|
| 100 elements | 0.40 ms | ~250,000 ops/sec |
| 1,000 elements | 6.8 ms | ~147 ops/sec |
| 5,000 elements | 145 ms | ~7 ops/sec |

### Output Format Generation

| Format | Document Size | Mean Time |
|--------|--------------|-----------|
| JSON | Small (100 el) | 0.18 ms |
| JSON | Medium (5,000 el) | 261 ms |
| HTML | Small (100 el) | 1.0 ms |
| HTML | Medium (5,000 el) | 770 ms |
| HTML + JSON | Small (100 el) | 1.3 ms |

### LCS Algorithm Performance

| Sequence Length | Mean Time | Algorithm |
|-----------------|-----------|-----------|
| 10 items | 7.8 us | Classic LCS |
| 100 items | 21.1 us | Classic LCS |
| 1,000 items | 2.90 ms | Classic LCS |
| 1,000 strings | 3.1 ms | With comparer |

### Memory Usage

- **Small diff (100 elements)**: ~300 MB allocated
- **Medium diff (5,000 elements)**: ~140 MB allocated
- **GC Gen 2 collections**: Minimal for typical workloads

### Performance Tips

1. **Use key attributes** for better matching on reordered elements
2. **Exclude attributes/nodes** you don't need to compare
3. **For very large files**, consider using async methods
4. **JSON generation** is significantly faster than HTML

**Benchmark Environment:**
- Runtime: .NET 9.0.10 (x64 RyuJIT AVX2)
- Hardware: Intel/AMD x64 with AVX2 support
- Build: Release optimization enabled

---

## Error Handling

The library may throw these exceptions:

| Exception | When Thrown | How to Handle |
|-----------|------------|---------------|
| `ArgumentException` | Paths contain traversal sequences or are null/empty | Validate input before calling |
| `FileNotFoundException` | Input files not found | Check file existence first |
| `XmlException` | File contains malformed XML | Validate XML or catch exception |
| `InvalidOperationException` | Invalid options JSON or file too large | Check file size and JSON format |
| `UnauthorizedAccessException` | No read permission | Check file permissions |

### Example Error Handling

```csharp
try
{
    var result = XmlComparer.CompareFilesWithReport(
        "original.xml",
        "new.xml",
        options => options.IncludeHtml());
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.FileName}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Operation failed: {ex.Message}");
}
```

---

## JSON Output Schema

The JSON output is an envelope:

```json
{
  "Validation": {
    "IsValid": true,
    "Errors": []
  },
  "Summary": {
    "Total": 10,
    "Unchanged": 5,
    "Added": 2,
    "Deleted": 1,
    "Modified": 1,
    "Moved": 1
  },
  "Diff": {
    "Name": "root",
    "Type": "Modified",
    "Children": [...]
  }
}
```

The JSON Schema for the envelope is packaged as `XmlDiff.schema.json` in the NuGet package.

---

## Projects

- `XmlComparer.Core` - Diff engine and HTML formatter
- `XmlComparer.Runner` - CLI/sample runner
- `XmlComparer.Tests` - Unit tests

## Configuration Options

| Option | Type | Description |
|--------|------|-------------|
| `ExcludedNodeNames` | `HashSet<string>` | Skip matching nodes by element name |
| `ExcludeSubtree` | `bool` | If true, excluded nodes and children are ignored; if false, only node is ignored |
| `ExcludedAttributeNames` | `HashSet<string>` | Ignore attributes by name |
| `KeyAttributeNames` | `HashSet<string>` | Attributes that uniquely identify sibling elements |
| `NormalizeWhitespace` | `bool` | Collapse runs of whitespace before comparing |
| `NormalizeNewlines` | `bool` | Treat CRLF and CR as LF |
| `TrimValues` | `bool` | Trim leading/trailing whitespace before comparing |
| `IgnoreValues` | `bool` | Ignore text values when comparing |

## Async and Streams

```csharp
using XmlComparer.Core;
using System.IO;
using System.Threading.Tasks;

// Async file comparison
var diff = await XmlComparer.CompareFilesAsync(@"C:\a.xml", @"C:\b.xml");

// Stream comparison
using var left = File.OpenRead(@"C:\a.xml");
using var right = File.OpenRead(@"C:\b.xml");
var diffFromStreams = XmlComparer.CompareStreams(left, right);
```

## String Extension Methods

```csharp
using XmlComparer.Core;

string xml1 = "<root><child>1</child></root>";
string xml2 = "<root><child>2</child></root>";

var diff = xml1.CompareXmlTo(xml2, options => options.IgnoreValues());
var report = xml1.CompareXmlToWithReport(xml2, options => options.IncludeHtml());
```

## Dependency Injection Friendly Client

```csharp
using XmlComparer.Core;

var client = new XmlComparerClient(
    new XmlDiffConfig(),
    new DefaultMatchingStrategy());

var result = client.CompareContentWithReport(
    "<root><child>1</child></root>",
    "<root><child>2</child></root>",
    options => options.IncludeJson());
```

## Embedded XSD Usage

If your schemas are embedded resources:

```csharp
using System.Reflection;
using XmlComparer.Core;

var assembly = Assembly.GetExecutingAssembly();
var resources = XmlSchemaSetFactory.ListEmbeddedSchemas(assembly);
var schemaSet = XmlSchemaSetFactory.FromEmbeddedResource(
    assembly,
    "Your.Assembly.Resources.SampleSchema.xsd");

var validator = new XmlSchemaValidator(schemaSet);
var result = validator.ValidateContent("<root><child>123</child></root>");
```

## Matching Strategy Registry

Register strategies once and resolve by id:

```csharp
XmlComparisonOptions.MatchingStrategyRegistry.TryRegister(
    "default",
    () => new DefaultMatchingStrategy());

loaded.ResolveMatchingStrategyFromRegistry();

if (XmlComparisonOptions.MatchingStrategyRegistry.TryResolve("default", out var strategy))
{
    // Use strategy
}
```

**Registry lifecycle tip:** Register strategies at app startup and clear/unregister on shutdown or in test teardown to avoid stale global state.
