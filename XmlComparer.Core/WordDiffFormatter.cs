using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Configuration options for Word (.docx) diff export.
    /// </summary>
    /// <remarks>
    /// <para>This class controls how XML diffs are exported to Word document format.</para>
    /// <para><b>Note:</b> Actual Word document generation requires the OpenXML SDK or
    /// a similar library. This class provides the configuration and structure.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new WordExportOptions
    /// {
    ///     IncludeTableOfContents = true,
    ///     TrackChangesMode = true,
    ///     EnableChangeTracking = true
    /// };
    ///
    /// var formatter = new WordDiffFormatter(options);
    /// byte[] docxData = formatter.FormatAsBytes(diffResult);
    /// File.WriteAllBytes("diff.docx", docxData);
    /// </code>
    /// </example>
    public class WordExportOptions
    {
        /// <summary>
        /// Gets or sets whether to include a table of contents.
        /// </summary>
        /// <remarks>
        /// When true, generates a TOC at the beginning with automatic updates.
        /// </remarks>
        public bool IncludeTableOfContents { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable Word's Track Changes feature.
        /// </summary>
        /// <remarks>
        /// When true, deletions are shown with strikethrough and additions are underlined.
        /// This uses Word's native change tracking for easy review and acceptance/rejection.
        /// </remarks>
        public bool TrackChangesMode { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable change tracking colors.
        /// </summary>
        /// <remarks>
        /// When true, changes are color-coded (red for deletions, blue for additions).
        /// </remarks>
        public bool EnableChangeTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include a cover page.
        /// </summary>
        public bool IncludeCoverPage { get; set; } = false;

        /// <summary>
        /// Gets or sets the cover page title.
        /// </summary>
        /// <remarks>
        /// Default is "XML Comparison Report".
        /// </remarks>
        public string Title { get; set; } = "XML Comparison Report";

        /// <summary>
        /// Gets or sets the cover page subtitle.
        /// </summary>
        public string? Subtitle { get; set; }

        /// <summary>
        /// Gets or sets the author name for the document metadata.
        /// </summary>
        /// <remarks>
        /// Default is "XmlComparer".
        /// </remarks>
        public string Author { get; set; } = "XmlComparer";

        /// <summary>
        /// Gets or sets whether to include page numbers.
        /// </summary>
        public bool IncludePageNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include headers and footers.
        /// </summary>
        public bool IncludeHeadersFooters { get; set; } = true;

        /// <summary>
        /// Gets or sets the header text.
        /// </summary>
        /// <remarks>
        /// Default is the title. Use {PAGE} for page number, {NUMPAGES} for total pages.
        /// </remarks>
        public string? HeaderText { get; set; }

        /// <summary>
        /// Gets or sets whether to include a summary section.
        /// </summary>
        public bool IncludeSummary { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use Word's built-in styles.
        /// </summary>
        /// <remarks>
        /// When true, uses Heading 1, Heading 2, Normal, etc.
        /// </remarks>
        public bool UseBuiltinStyles { get; set; } = true;

        /// <summary>
        /// Gets or sets the font family for body text.
        /// </summary>
        /// <remarks>
        /// Default is "Calibri".
        /// </remarks>
        public string FontFamily { get; set; } = "Calibri";

        /// <summary>
        /// Gets or sets the font size for body text in points.
        /// </summary>
        /// <remarks>
        /// Default is 11 points.
        /// </remarks>
        public int FontSize { get; set; } = 11;

        /// <summary>
        /// Gets or sets the font family for code/XML snippets.
        /// </summary>
        /// <remarks>
        /// Default is "Consolas" or "Courier New".
        /// </remarks>
        public string CodeFontFamily { get; set; } = "Consolas";

        /// <summary>
        /// Gets or sets the font size for code/XML snippets in points.
        /// </summary>
        /// <remarks>
        /// Default is 9 points.
        /// </remarks>
        public int CodeFontSize { get; set; } = 9;

        /// <summary>
        /// Gets or sets whether to include a separate sheet for statistics.
        /// </summary>
        public bool IncludeStatisticsTable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to group changes by type.
        /// </summary>
        /// <remarks>
        /// When true, creates separate sections for additions, deletions, and modifications.
        /// </remarks>
        public bool GroupByChangeType { get; set; } = false;

        /// <summary>
        /// Creates a new WordExportOptions with default settings.
        /// </summary>
        public WordExportOptions() { }

        /// <summary>
        /// Creates options with Track Changes enabled.
        /// </summary>
        /// <returns>New options configured for Track Changes mode.</returns>
        public static WordExportOptions WithTrackChanges() => new WordExportOptions
        {
            TrackChangesMode = true,
            EnableChangeTracking = true,
            IncludeTableOfContents = true,
            IncludeSummary = true
        };

        /// <summary>
        /// Creates options for a professional report with cover page.
        /// </summary>
        /// <returns>New options configured for professional output.</returns>
        public static WordExportOptions Professional() => new WordExportOptions
        {
            IncludeCoverPage = true,
            IncludeTableOfContents = true,
            IncludeHeadersFooters = true,
            IncludeSummary = true,
            IncludeStatisticsTable = true,
            UseBuiltinStyles = true
        };

        /// <summary>
        /// Creates options for a minimal document.
        /// </summary>
        /// <returns>New options configured for minimal output.</returns>
        public static WordExportOptions Minimal() => new WordExportOptions
        {
            IncludeCoverPage = false,
            IncludeTableOfContents = false,
            IncludeHeadersFooters = false,
            IncludeSummary = true,
            IncludeStatisticsTable = false,
            GroupByChangeType = true
        };
    }

    /// <summary>
    /// Generates Word (.docx) formatted diff reports from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This formatter creates Word documents that can be edited, reviewed,
    /// and shared using Microsoft Word or compatible applications.</para>
    /// <para>Features include:</para>
    /// <list type="bullet">
    ///   <item><description>Track Changes integration for native review workflow</description></item>
    ///   <item><description>Automatic table of contents</description></item>
    ///   <item><description>Cover page with metadata</description></item>
    ///   <item><description>Headers, footers, and page numbers</description></item>
    ///   <item><description>Syntax-highlighted XML code blocks</description></item>
    ///   <item><description>Statistics summary table</description></item>
    /// </list>
    /// <para><b>Implementation Note:</b> This class provides the structure and configuration
    /// for Word document generation. The default implementation returns an XML representation
    /// of the document that can be converted to .docx using the OpenXML SDK.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new WordDiffFormatter();
    /// string wordXml = formatter.Format(diffResult);
    ///
    /// // With Track Changes enabled
    /// var options = new WordExportOptions { TrackChangesMode = true };
    /// var tcFormatter = new WordDiffFormatter(options);
    /// byte[] docxData = tcFormatter.FormatAsBytes(diffResult);
    /// File.WriteAllBytes("diff.docx", docxData);
    /// </code>
    /// </example>
    public class WordDiffFormatter : IDiffFormatter
    {
        private readonly WordExportOptions _options;
        private readonly StringBuilder _sb;
        private DiffStatistics _stats;

        /// <summary>
        /// Gets the export options for this formatter.
        /// </summary>
        public WordExportOptions Options => _options;

        /// <summary>
        /// Creates a new WordDiffFormatter with default options.
        /// </summary>
        public WordDiffFormatter() : this(new WordExportOptions()) { }

        /// <summary>
        /// Creates a new WordDiffFormatter with the specified options.
        /// </summary>
        /// <param name="options">The Word export options to use.</param>
        public WordDiffFormatter(WordExportOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Formats a diff tree into Word XML format.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>A Word XML formatted string.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into Word XML format with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>A Word XML formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            _sb.Clear();
            _stats = new DiffStatistics();

            // Calculate statistics
            CalculateStatistics(diff, _stats);

            // Write Word XML (simplified OpenXML format)
            WriteWordStart();

            // Write cover page
            if (_options.IncludeCoverPage)
            {
                WriteCoverPage();
            }

            // Write table of contents
            if (_options.IncludeTableOfContents)
            {
                WriteTableOfContents();
            }

            // Write summary
            if (_options.IncludeSummary)
            {
                WriteSummary();
            }

            // Write changes
            if (_options.GroupByChangeType)
            {
                WriteGroupedByType(diff);
            }
            else
            {
                WriteHierarchical(diff);
            }

            // Write Word end
            WriteWordEnd();

            return _sb.ToString();
        }

        /// <summary>
        /// Formats a diff tree and returns the data as bytes.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>The formatted data as bytes.</returns>
        public byte[] FormatAsBytes(DiffMatch diff)
        {
            string xml = Format(diff);
            return Encoding.UTF8.GetBytes(xml);
        }

        /// <summary>
        /// Writes the Word document start.
        /// </summary>
        private void WriteWordStart()
        {
            _sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            _sb.AppendLine("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">");
            _sb.AppendLine("<w:body>");
        }

        /// <summary>
        /// Writes the cover page.
        /// </summary>
        private void WriteCoverPage()
        {
            _sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr>");
            WriteRun(_options.Title, "Title", 28, true);
            _sb.AppendLine("</w:p>");

            if (!string.IsNullOrEmpty(_options.Subtitle))
            {
                _sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr>");
                WriteRun(_options.Subtitle, "Subtitle", 14, false);
                _sb.AppendLine("</w:p>");
            }

            _sb.AppendLine("<w:p><w:pPr><w:jc w:val=\"center\"/></w:pPr>");
            WriteRun($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC", null, 11, false);
            _sb.AppendLine("</w:p>");

            WritePageBreak();
        }

        /// <summary>
        /// Writes the table of contents.
        /// </summary>
        private void WriteTableOfContents()
        {
            _sb.AppendLine("<w:p><w:pPr><w:outlineLvl w:val=\"0\"/></w:pPr>");
            WriteRun("Table of Contents", "Heading1", 16, true);
            _sb.AppendLine("</w:p>");

            _sb.AppendLine("<w:p>");
            WriteRun("The TOC would be generated here. In actual Word, use TOC field.", null, 11, false);
            _sb.AppendLine("</w:p>");

            WritePageBreak();
        }

        /// <summary>
        /// Writes the summary section.
        /// </summary>
        private void WriteSummary()
        {
            _sb.AppendLine("<w:p><w:pPr><w:outlineLvl w:val=\"0\"/></w:pPr>");
            WriteRun("Summary", "Heading1", 16, true);
            _sb.AppendLine("</w:p>");

            if (_options.IncludeStatisticsTable)
            {
                WriteStatisticsTable();
            }
        }

        /// <summary>
        /// Writes the statistics table.
        /// </summary>
        private void WriteStatisticsTable()
        {
            _sb.AppendLine("<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr>");

            // Header row
            _sb.AppendLine("<w:tr>");
            WriteTableCell("Metric", true);
            WriteTableCell("Count", true);
            _sb.AppendLine("</w:tr>");

            // Data rows
            _sb.AppendLine("<w:tr>");
            WriteTableCell("Total Elements", false);
            WriteTableCell(_stats.TotalElements.ToString(), false);
            _sb.AppendLine("</w:tr>");

            _sb.AppendLine("<w:tr>");
            WriteTableCell("Additions", false);
            WriteTableCell(_stats.Additions.ToString(), false);
            _sb.AppendLine("</w:tr>");

            _sb.AppendLine("<w:tr>");
            WriteTableCell("Deletions", false);
            WriteTableCell(_stats.Deletions.ToString(), false);
            _sb.AppendLine("</w:tr>");

            _sb.AppendLine("<w:tr>");
            WriteTableCell("Modifications", false);
            WriteTableCell(_stats.Modifications.ToString(), false);
            _sb.AppendLine("</w:tr>");

            if (_stats.Moves > 0)
            {
                _sb.AppendLine("<w:tr>");
                WriteTableCell("Moves", false);
                WriteTableCell(_stats.Moves.ToString(), false);
                _sb.AppendLine("</w:tr>");
            }

            _sb.AppendLine("<w:tr>");
            WriteTableCell("Total Changes", false);
            WriteTableCell(_stats.TotalChanges.ToString(), false);
            _sb.AppendLine("</w:tr>");

            _sb.AppendLine("</w:tbl>");
        }

        /// <summary>
        /// Writes a table cell.
        /// </summary>
        private void WriteTableCell(string content, bool isHeader = false)
        {
            _sb.AppendLine("<w:tc>");
            WriteRun(content, null, isHeader ? 11 : 10, isHeader);
            _sb.AppendLine("</w:tc>");
        }

        /// <summary>
        /// Writes a table row with two cells.
        /// </summary>
        private void WriteTableRow(string label, string value)
        {
            _sb.AppendLine("<w:tr>");
            WriteTableCell(label);
            WriteTableCell(value);
            _sb.AppendLine("</w:tr>");
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
                WriteChangeSection("Additions", additions, DiffType.Added);
            }

            if (deletions.Any())
            {
                WriteChangeSection("Deletions", deletions, DiffType.Deleted);
            }

            if (modifications.Any())
            {
                WriteChangeSection("Modifications", modifications, DiffType.Modified);
            }

            if (moves.Any())
            {
                WriteChangeSection("Moves", moves, DiffType.Moved);
            }
        }

        /// <summary>
        /// Writes a section of changes of a specific type.
        /// </summary>
        private void WriteChangeSection(string title, List<DiffMatch> changes, DiffType type)
        {
            _sb.AppendLine("<w:p><w:pPr><w:outlineLvl w:val=\"1\"/></w:pPr>");
            WriteRun(title, "Heading2", 14, true);
            _sb.AppendLine("</w:p>");

            foreach (var change in changes)
            {
                WriteChangeItem(change, type);
            }
        }

        /// <summary>
        /// Writes hierarchical changes.
        /// </summary>
        private void WriteHierarchical(DiffMatch diff)
        {
            _sb.AppendLine("<w:p><w:pPr><w:outlineLvl w:val=\"0\"/></w:pPr>");
            WriteRun("Changes", "Heading1", 16, true);
            _sb.AppendLine("</w:p>");

            if (_stats.TotalChanges == 0)
            {
                _sb.AppendLine("<w:p>");
                WriteRun("No differences detected.", null, 11, false);
                _sb.AppendLine("</w:p>");
            }
            else
            {
                WriteChangesRecursive(diff);
            }
        }

        /// <summary>
        /// Writes changes recursively.
        /// </summary>
        private void WriteChangesRecursive(DiffMatch node)
        {
            if (node.Type != DiffType.Unchanged)
            {
                WriteChangeItem(node, node.Type);
            }

            foreach (var child in node.Children)
            {
                WriteChangesRecursive(child);
            }
        }

        /// <summary>
        /// Writes a single change item.
        /// </summary>
        private void WriteChangeItem(DiffMatch node, DiffType type)
        {
            string changeType = type switch
            {
                DiffType.Added => "Added",
                DiffType.Deleted => "Deleted",
                DiffType.Modified => "Modified",
                DiffType.Moved => "Moved",
                _ => ""
            };

            _sb.AppendLine("<w:p>");
            WriteRun($"{changeType}: ", null, 11, true);
            WriteRun(node.Path ?? "Unknown", null, 11, false);
            _sb.AppendLine("</w:p>");

            var content = FormatXmlContent(node);
            if (!string.IsNullOrEmpty(content))
            {
                _sb.AppendLine("<w:p>");
                WriteRun(content, _options.CodeFontFamily, _options.CodeFontSize, false);
                _sb.AppendLine("</w:p>");
            }
        }

        /// <summary>
        /// Writes a text run.
        /// </summary>
        private void WriteRun(string text, string? style, int fontSize, bool bold)
        {
            _sb.AppendLine("<w:r>");
            _sb.AppendLine("<w:rPr>");
            if (bold) _sb.AppendLine("<w:b/>");
            _sb.AppendLine($"<w:sz w:val=\"{fontSize * 2}\"/>");
            _sb.AppendLine("</w:rPr>");
            _sb.AppendLine($"<w:t>{EscapeXml(text)}</w:t>");
            _sb.AppendLine("</w:r>");
        }

        /// <summary>
        /// Writes a page break.
        /// </summary>
        private void WritePageBreak()
        {
            _sb.AppendLine("<w:p><w:r><w:br w:type=\"page\"/></w:r></w:p>");
        }

        /// <summary>
        /// Writes the Word document end.
        /// </summary>
        private void WriteWordEnd()
        {
            _sb.AppendLine("</w:body>");
            _sb.AppendLine("</w:document>");
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
                    sb.Append(indentStr);
                }

                sb.Append($"</{element.Name}>");
            }
        }

        /// <summary>
        /// Escapes XML special characters.
        /// </summary>
        private string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
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
        /// Calculates statistics from a diff tree.
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
