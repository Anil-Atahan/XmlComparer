# Contributing to XmlComparer

Thank you for your interest in contributing to **XmlComparer**, a zero‑dependency .NET library and CLI for structural XML comparison. Contributions of all kinds are welcome: bug reports, documentation improvements, tests, performance tweaks, and new features.

## Repository Layout

- `XmlComparer.Core` – core diff engine, formatters, and public APIs
- `XmlComparer.Runner` – CLI / sample runner used in docs and examples
- `XmlComparer.Tests` – unit and integration tests
- `XmlComparer.Benchmarks` – performance benchmarks

## Getting Started

1. **Fork** the repository on GitHub.
2. **Clone** your fork:
    ```bash
    git clone https://github.com/<your-username>/XmlComparer.git
    ```
3. Install the .NET SDK version listed in the README (currently .NET 9.0 or later).
4. Open `XmlComparer.sln` in Visual Studio / Rider or work from the command line.

### Building and Testing

- Build everything:
   ```bash
   dotnet build XmlComparer.sln -c Release
   ```
- Run tests (recommended before every PR):
   ```bash
   dotnet test XmlComparer.sln
   ```
- Optionally run benchmarks (may take longer):
   ```bash
   dotnet run -c Release -p XmlComparer.Benchmarks/XmlComparer.Benchmarks.csproj
   ```

## Types of Contributions

- **Bug reports** – Include minimal XML samples, the exact API/CLI command used, expected behavior, and actual behavior.
- **Feature requests** – Describe the XML comparison scenario (e.g., config files, documents, schemas) and how a new feature would help.
- **Pull requests** – Small, focused changes are easier to review and merge. If you plan a big change to comparison semantics or output formats, please open an issue for discussion first.

## Pull Request Guidelines

- Create a feature branch from `main`.
- Keep changes focused and avoid unrelated refactoring.
- When modifying comparison logic, try to add:
   - A unit test in `XmlComparer.Tests` that captures the behavior.
   - A small XML sample that clearly shows the diff.
- Ensure `dotnet test` passes locally before opening the PR.
- Update documentation when needed (README, XML comments, or runner help text).

## Coding Style

- Follow the existing coding style in each project (naming, spacing, and patterns).
- Prefer descriptive names for types and members (e.g., `XmlComparisonOptions` rather than abbreviations).
- Avoid introducing new third‑party dependencies unless discussed in an issue.
- Keep public API changes minimal and clearly documented.

## XML and Security Considerations

XmlComparer is often used on untrusted XML. When changing parsing, schema validation, or security‑related code (e.g., XML reader settings, DTD handling, external entities):

- Preserve safe defaults (no dangerous entity expansion, no unexpected network/file access).
- Add tests for edge cases like large documents, deeply nested elements, or tricky namespaces.

## Reporting Security Issues

If you discover a security vulnerability, **do not** open a public issue. Instead, follow the process described in `SECURITY.md`.

Thank you again for helping make XmlComparer better!
