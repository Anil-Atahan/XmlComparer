using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates markdown formatted diff reports from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This formatter creates human-readable markdown reports that can be viewed
    /// on GitHub, GitLab, Bitbucket, or any markdown renderer.</para>
    /// <para>The output includes:</para>
    /// <list type="bullet">
    ///   <item><description>Document header with title and metadata</description></item>
    ///   <item><description>Summary statistics (additions, deletions, modifications)</description></item>
    ///   <item><description>Detailed changes with syntax-highlighted XML snippets</description></item>
    ///   <item><description>Table of contents (optional)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new MarkdownDiffFormatter();
    /// string markdown = formatter.Format(diffResult);
    ///
    /// // With custom styling
    /// var style = new MarkdownStyle
    /// {
    ///     Flavor = MarkdownFlavor.GitHub,
    ///     UseEmoji = true,
    ///     IncludeTableOfContents = true
    /// };
    /// var customFormatter = new MarkdownDiffFormatter(style);
    /// string customMarkdown = customFormatter.Format(diffResult);
    /// </code>
    /// </example>
    public class MarkdownDiffFormatter : IDiffFormatter
    {
        private readonly MarkdownStyle _style;
        private readonly StringBuilder _sb;
        private DiffStatistics _stats;

        /// <summary>
        /// Gets or sets the style configuration for this formatter.
        /// </summary>
        public MarkdownStyle Style { get => _style; }

        /// <summary>
        /// Creates a new MarkdownDiffFormatter with default settings.
        /// </summary>
        public MarkdownDiffFormatter() : this(new MarkdownStyle()) { }

        /// <summary>
        /// Creates a new MarkdownDiffFormatter with the specified style.
        /// </summary>
        /// <param name="style">The style configuration to use.</param>
        public MarkdownDiffFormatter(MarkdownStyle style)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Formats a diff tree into a markdown string.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>A markdown formatted string.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into a markdown string with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>A markdown formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            _sb.Clear();
            _stats = new DiffStatistics();

            // Calculate statistics
            CalculateStatistics(diff, _stats);

            // Write header
            WriteHeader();

            // Write table of contents
            if (_style.IncludeTableOfContents)
            {
                WriteTableOfContents();
            }

            // Write statistics
            if (_style.IncludeStatistics)
            {
                WriteStatistics();
            }

            // Write validation results if present
            if (context?.ValidationResult != null)
            {
                WriteValidationResults(context.ValidationResult);
            }

            // Write changes based on detail level
            if (_style.DetailLevel == MarkdownDetailLevel.Summary)
            {
                WriteSummary(diff);
            }
            else if (_style.GroupByChangeType)
            {
                WriteGroupedByType(diff);
            }
            else
            {
                WriteHierarchical(diff, 0);
            }

            // Write footer
            WriteFooter();

            return _sb.ToString();
        }

        /// <summary>
        /// Writes the document header.
        /// </summary>
        private void WriteHeader()
        {
            _sb.AppendLine($"# {_style.Title}");

            if (!string.IsNullOrEmpty(_style.Subtitle))
            {
                _sb.AppendLine($"## {_style.Subtitle}");
            }

            _sb.AppendLine();

            if (_style.IncludeTimestamp)
            {
                _sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                _sb.AppendLine();
            }

            if (_style.Flavor != MarkdownFlavor.Standard)
            {
                _sb.AppendLine("---");
                _sb.AppendLine();
            }
        }

        /// <summary>
        /// Writes the table of contents.
        /// </summary>
        private void WriteTableOfContents()
        {
            _sb.AppendLine("## Table of Contents");
            _sb.AppendLine();

            if (_style.IncludeStatistics)
            {
                _sb.AppendLine("- [Statistics](#statistics)");
            }

            if (_style.DetailLevel != MarkdownDetailLevel.Summary)
            {
                if (_style.GroupByChangeType)
                {
                    _sb.AppendLine("- [Additions](#additions)");
                    _sb.AppendLine("- [Deletions](#deletions)");
                    _sb.AppendLine("- [Modifications](#modifications)");
                    if (_style.Flavor == MarkdownFlavor.GitLab || _style.Flavor == MarkdownFlavor.GitHub)
                    {
                        _sb.AppendLine("- [Moves](#moves)");
                    }
                }
                else
                {
                    _sb.AppendLine("- [Changes](#changes)");
                }
            }

            _sb.AppendLine();
            _sb.AppendLine("---");
            _sb.AppendLine();
        }

        /// <summary>
        /// Writes the statistics section.
        /// </summary>
        private void WriteStatistics()
        {
            _sb.AppendLine("## Statistics");
            _sb.AppendLine();

            if (_style.Flavor == MarkdownFlavor.GitHub || _style.Flavor == MarkdownFlavor.GitLab)
            {
                // Use emoji for flavored markdown
                _sb.AppendLine($"| Metric | Count |");
                _sb.AppendLine($"|--------|-------|");
                _sb.AppendLine($"| ✅ Elements Compared | {_stats.TotalElements} |");
                _sb.AppendLine($"| ➕ Additions | {_stats.Additions} |");
                _sb.AppendLine($"| ➖ Deletions | {_stats.Deletions} |");
                _sb.AppendLine($"| 🔄 Modifications | {_stats.Modifications} |");
                if (_stats.Moves > 0)
                    _sb.AppendLine($"| ↪️ Moves | {_stats.Moves} |");
            }
            else
            {
                _sb.AppendLine($"- **Elements Compared:** {_stats.TotalElements}");
                _sb.AppendLine($"- **Additions:** {_stats.Additions}");
                _sb.AppendLine($"- **Deletions:** {_stats.Deletions}");
                _sb.AppendLine($"- **Modifications:** {_stats.Modifications}");
                if (_stats.Moves > 0)
                    _sb.AppendLine($"- **Moves:** {_stats.Moves}");
            }

            _sb.AppendLine();
            _sb.AppendLine("---");
            _sb.AppendLine();
        }

        /// <summary>
        /// Writes validation results if available.
        /// </summary>
        private void WriteValidationResults(XmlValidationResult validationResult)
        {
            _sb.AppendLine("## Validation Results");
            _sb.AppendLine();

            if (validationResult.IsValid)
            {
                _sb.AppendLine("✅ Both XML documents are valid.");
            }
            else
            {
                _sb.AppendLine("⚠️ Validation issues detected:");

                if (validationResult.Errors.Any())
                {
                    _sb.AppendLine();
                    foreach (var error in validationResult.Errors)
                    {
                        _sb.AppendLine($"- {error.Message} (Line {error.LineNumber}, Column {error.LinePosition})");
                    }
                }
            }

            _sb.AppendLine();
            _sb.AppendLine("---");
            _sb.AppendLine();
        }

        /// <summary>
        /// Writes a summary view (statistics only).
        /// </summary>
        private void WriteSummary(DiffMatch diff)
        {
            _sb.AppendLine("## Summary");
            _sb.AppendLine();

            if (_stats.TotalChanges == 0)
            {
                _sb.AppendLine("No differences detected between the XML documents.");
            }
            else
            {
                _sb.AppendLine($"The XML documents differ in {_stats.TotalChanges} place(s).");
                _sb.AppendLine();

                if (_style.UseEmoji)
                {
                    _sb.AppendLine($"- ➕ {_stats.Additions} addition(s)");
                    _sb.AppendLine($"- ➖ {_stats.Deletions} deletion(s)");
                    _sb.AppendLine($"- 🔄 {_stats.Modifications} modification(s)");
                    if (_stats.Moves > 0)
                        _sb.AppendLine($"- ↪️ {_stats.Moves} move(s)");
                }
                else
                {
                    _sb.AppendLine($"- {_stats.Additions} addition(s)");
                    _sb.AppendLine($"- {_stats.Deletions} deletion(s)");
                    _sb.AppendLine($"- {_stats.Modifications} modification(s)");
                    if (_stats.Moves > 0)
                        _sb.AppendLine($"- {_stats.Moves} move(s)");
                }
            }
        }

        /// <summary>
        /// Writes changes grouped by type.
        /// </summary>
        private void WriteGroupedByType(DiffMatch diff)
        {
            var additions = new List<DiffMatch>();
            var deletions = new List<DiffMatch>();
            var modifications = new List<DiffMatch>();
            var moves = new List<DiffMatch>();

            CollectChangesByType(diff, additions, deletions, modifications, moves);

            if (additions.Any())
            {
                WriteSection("Additions", "➕", additions, DiffType.Added);
            }

            if (deletions.Any())
            {
                WriteSection("Deletions", "➖", deletions, DiffType.Deleted);
            }

            if (modifications.Any())
            {
                WriteSection("Modifications", "🔄", modifications, DiffType.Modified);
            }

            if (moves.Any())
            {
                WriteSection("Moves", "↪️", moves, DiffType.Moved);
            }
        }

        /// <summary>
        /// Writes a single section of changes.
        /// </summary>
        private void WriteSection(string title, string emoji, List<DiffMatch> changes, DiffType type)
        {
            string anchor = title.ToLowerInvariant();
            _sb.AppendLine($"## {title} <a name=\"{anchor}\"></a>");
            _sb.AppendLine();

            foreach (var change in changes)
            {
                WriteChange(change, type);
            }

            _sb.AppendLine();
        }

        /// <summary>
        /// Collects changes grouped by type.
        /// </summary>
        private void CollectChangesByType(
            DiffMatch node,
            List<DiffMatch> additions,
            List<DiffMatch> deletions,
            List<DiffMatch> modifications,
            List<DiffMatch> moves)
        {
            if (node.Type == DiffType.Added)
                additions.Add(node);
            else if (node.Type == DiffType.Deleted)
                deletions.Add(node);
            else if (node.Type == DiffType.Modified)
                modifications.Add(node);
            else if (node.Type == DiffType.Moved)
                moves.Add(node);

            foreach (var child in node.Children)
            {
                CollectChangesByType(child, additions, deletions, modifications, moves);
            }
        }

        /// <summary>
        /// Writes changes in hierarchical format.
        /// </summary>
        private void WriteHierarchical(DiffMatch diff, int depth)
        {
            if (_style.DetailLevel == MarkdownDetailLevel.Compact)
            {
                WriteCompact(diff, 0);
                return;
            }

            _sb.AppendLine("## Changes");
            _sb.AppendLine();

            if (_stats.TotalChanges == 0)
            {
                _sb.AppendLine("No differences detected.");
                return;
            }

            WriteNodeRecursive(diff, 1);
        }

        /// <summary>
        /// Writes a node and its children recursively.
        /// </summary>
        private void WriteNodeRecursive(DiffMatch node, int depth)
        {
            // Check max depth
            if (_style.MaxDepth > 0 && depth > _style.MaxDepth)
            {
                return;
            }

            // Only write changed nodes
            if (node.Type != DiffType.Unchanged)
            {
                WriteChange(node, node.Type);
            }

            // Process children
            foreach (var child in node.Children)
            {
                WriteNodeRecursive(child, depth + 1);
            }
        }

        /// <summary>
        /// Writes a compact view (paths only).
        /// </summary>
        private void WriteCompact(DiffMatch diff, int depth)
        {
            _sb.AppendLine("## Changes");
            _sb.AppendLine();

            var paths = new List<string>();
            CollectPaths(diff, paths, "");

            foreach (var path in paths.OrderBy(p => p))
            {
                _sb.AppendLine($"- {path}");
            }
        }

        /// <summary>
        /// Collects all changed paths.
        /// </summary>
        private void CollectPaths(DiffMatch node, List<string> paths, string currentPath)
        {
            string nodePath = string.IsNullOrEmpty(currentPath)
                ? $"/{node.Path}"
                : $"{currentPath}/{node.Path}";

            if (node.Type != DiffType.Unchanged)
            {
                string indicator = node.Type switch
                {
                    DiffType.Added => _style.UseEmoji ? "➕" : "+",
                    DiffType.Deleted => _style.UseEmoji ? "➖" : "-",
                    DiffType.Modified => _style.UseEmoji ? "🔄" : "~",
                    DiffType.Moved => _style.UseEmoji ? "↪️" : ">>",
                    _ => ""
                };
                paths.Add($"{indicator} `{nodePath}`");
            }

            foreach (var child in node.Children)
            {
                CollectPaths(child, paths, nodePath);
            }
        }

        /// <summary>
        /// Writes a single change with XML content.
        /// </summary>
        private void WriteChange(DiffMatch node, DiffType type)
        {
            string indicator = type switch
            {
                DiffType.Added => _style.UseEmoji ? "➕" : "Added",
                DiffType.Deleted => _style.UseEmoji ? "➖" : "Deleted",
                DiffType.Modified => _style.UseEmoji ? "🔄" : "Modified",
                DiffType.Moved => _style.UseEmoji ? "↪️" : "Moved",
                _ => ""
            };

            _sb.AppendLine($"### {indicator} `{node.Path ?? "Unknown"}`");
            _sb.AppendLine();

            if (_style.DetailLevel == MarkdownDetailLevel.Full && node.Type != DiffType.Unchanged)
            {
                string content = FormatXmlContent(node);
                _sb.AppendLine($"```{_style.GetCodeLanguage()}");
                _sb.AppendLine(content);
                _sb.AppendLine($"```");
                _sb.AppendLine();
            }
        }

        /// <summary>
        /// Formats the XML content of a diff node.
        /// </summary>
        private string FormatXmlContent(DiffMatch node)
        {
            var element = node.NewElement ?? node.OriginalElement;
            if (element == null)
            {
                return $"<!-- {node.Type}: {node.Path} -->";
            }

            var sb = new StringBuilder();
            WriteXmlElement(node, sb, 0);
            return sb.ToString();
        }

        /// <summary>
        /// Writes an XML element with indentation.
        /// </summary>
        private void WriteXmlElement(DiffMatch node, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent);
            var element = node.NewElement ?? node.OriginalElement;

            if (element == null)
            {
                string name = node.Path?.Split('/').LastOrDefault() ?? "element";
                sb.Append($"{indentStr}<!-- {node.Type}: {name} -->");
                return;
            }

            sb.Append($"{indentStr}<{element.Name}");

            // Add attributes
            foreach (var attr in element.Attributes())
            {
                sb.Append($" {attr.Name}=\"{attr.Value}\"");
            }

            if (!element.HasElements && string.IsNullOrEmpty(element.Value))
            {
                sb.Append("/>");
            }
            else
            {
                sb.Append(">");

                if (!string.IsNullOrEmpty(element.Value))
                {
                    sb.Append(element.Value);
                }

                // Write children recursively (only direct children from the diff)
                bool hasChildren = false;
                foreach (var child in node.Children)
                {
                    if (child.Type != DiffType.Unchanged)
                    {
                        sb.AppendLine();
                        WriteXmlElement(child, sb, indent + 2);
                        hasChildren = true;
                    }
                }

                if (hasChildren)
                {
                    sb.AppendLine();
                    sb.Append($"{indentStr}");
                }

                sb.Append($"</{element.Name}>");
            }
        }

        /// <summary>
        /// Writes the document footer.
        /// </summary>
        private void WriteFooter()
        {
            if (_style.Flavor != MarkdownFlavor.Standard)
            {
                _sb.AppendLine("---");
                _sb.AppendLine();
            }

            _sb.AppendLine($"*Generated by [XmlComparer](https://github.com/yourusername/XmlComparer)*");
        }

        /// <summary>
        /// Calculates diff statistics from a diff tree.
        /// </summary>
        private void CalculateStatistics(DiffMatch node, DiffStatistics stats)
        {
            stats.TotalElements++;

            switch (node.Type)
            {
                case DiffType.Added:
                    stats.Additions++;
                    break;
                case DiffType.Deleted:
                    stats.Deletions++;
                    break;
                case DiffType.Modified:
                    stats.Modifications++;
                    break;
                case DiffType.Moved:
                    stats.Moves++;
                    break;
            }

            foreach (var child in node.Children)
            {
                CalculateStatistics(child, stats);
            }
        }

        /// <summary>
        /// Statistics for a diff result.
        /// </summary>
        private class DiffStatistics
        {
            public int TotalElements { get; set; }
            public int Additions { get; set; }
            public int Deletions { get; set; }
            public int Modifications { get; set; }
            public int Moves { get; set; }
            public int TotalChanges => Additions + Deletions + Modifications + Moves;
        }
    }
}
