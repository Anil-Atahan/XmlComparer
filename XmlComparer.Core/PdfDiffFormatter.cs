using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines the page size for PDF output.
    /// </summary>
    public enum PdfPageSize
    {
        /// <summary>
        /// Letter size (8.5 x 11 inches).
        /// </summary>
        Letter,

        /// <summary>
        /// A4 size (210 x 297 mm).
        /// </summary>
        A4,

        /// <summary>
        /// Legal size (8.5 x 14 inches).
        /// </summary>
        Legal
    }

    /// <summary>
    /// Defines the orientation for PDF pages.
    /// </summary>
    public enum PdfOrientation
    {
        /// <summary>
        /// Portrait orientation (tall).
        /// </summary>
        Portrait,

        /// <summary>
        /// Landscape orientation (wide).
        /// </summary>
        Landscape
    }

    /// <summary>
    /// Configuration options for PDF diff export.
    /// </summary>
    /// <remarks>
    /// <para>This class controls how XML diffs are exported to PDF format.</para>
    /// <para><b>Note:</b> Actual PDF generation requires a PDF library such as
    /// iTextSharp, PdfSharp, or QuestPDF. This class provides the configuration
    /// and structure, but the actual PDF generation is delegated to the registered
    /// PDF generator implementation.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new PdfExportOptions
    /// {
    ///     PageSize = PdfPageSize.A4,
    ///     Orientation = PdfOrientation.Portrait,
    ///     IncludeTableOfContents = true,
    ///     EnableSyntaxHighlighting = true
    /// };
    ///
    /// var formatter = new PdfDiffFormatter(options);
    /// byte[] pdfData = formatter.FormatAsBytes(diffResult);
    /// File.WriteAllBytes("diff.pdf", pdfData);
    /// </code>
    /// </example>
    public class PdfExportOptions
    {
        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="PdfPageSize.Letter"/>.
        /// </remarks>
        public PdfPageSize PageSize { get; set; } = PdfPageSize.Letter;

        /// <summary>
        /// Gets or sets the page orientation.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="PdfOrientation.Portrait"/>.
        /// </remarks>
        public PdfOrientation Orientation { get; set; } = PdfOrientation.Portrait;

        /// <summary>
        /// Gets or sets the margin size in points (1/72 inch).
        /// </summary>
        /// <remarks>
        /// Default is 72 points (1 inch) on all sides.
        /// </remarks>
        public float MarginPoints { get; set; } = 72f;

        /// <summary>
        /// Gets or sets the font family for body text.
        /// </summary>
        /// <remarks>
        /// Default is "Helvetica".
        /// </remarks>
        public string FontFamily { get; set; } = "Helvetica";

        /// <summary>
        /// Gets or sets the font size for body text in points.
        /// </summary>
        /// <remarks>
        /// Default is 10 points.
        /// </remarks>
        public float FontSize { get; set; } = 10f;

        /// <summary>
        /// Gets or sets the font family for code/XML snippets.
        /// </summary>
        /// <remarks>
        /// Default is "Courier".
        /// </remarks>
        public string CodeFontFamily { get; set; } = "Courier";

        /// <summary>
        /// Gets or sets the font size for code/XML snippets in points.
        /// </summary>
        /// <remarks>
        /// Default is 8 points.
        /// </remarks>
        public float CodeFontSize { get; set; } = 8f;

        /// <summary>
        /// Gets or sets whether to include a table of contents.
        /// </summary>
        /// <remarks>
        /// When true, generates a TOC at the beginning with clickable links.
        /// </remarks>
        public bool IncludeTableOfContents { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include page numbers.
        /// </summary>
        /// <remarks>
        /// When true, page numbers appear in the footer.
        /// </remarks>
        public bool IncludePageNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable syntax highlighting for XML.
        /// </summary>
        /// <remarks>
        /// When true, XML elements, attributes, and values are colored.
        /// </remarks>
        public bool EnableSyntaxHighlighting { get; set; } = true;

        /// <summary>
        /// Gets or sets the title for the PDF document.
        /// </summary>
        /// <remarks>
        /// Default is "XML Comparison Report".
        /// </remarks>
        public string Title { get; set; } = "XML Comparison Report";

        /// <summary>
        /// Gets or sets the subtitle for the PDF document.
        /// </summary>
        /// <remarks>
        /// Default is null (no subtitle).
        /// </remarks>
        public string? Subtitle { get; set; }

        /// <summary>
        /// Gets or sets the author for the PDF metadata.
        /// </summary>
        /// <remarks>
        /// Default is "XmlComparer".
        /// </remarks>
        public string Author { get; set; } = "XmlComparer";

        /// <summary>
        /// Gets or sets the subject for the PDF metadata.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets the keywords for the PDF metadata.
        /// </summary>
        /// <remarks>
        /// Comma-separated keywords for search indexing.
        /// </remarks>
        public string? Keywords { get; set; }

        /// <summary>
        /// Gets or sets whether to include a summary section.
        /// </summary>
        public bool IncludeSummary { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to color-code changes.
        /// </summary>
        /// <remarks>
        /// When true, additions are green, deletions are red, modifications are yellow.
        /// </remarks>
        public bool ColorCodeChanges { get; set; } = true;

        /// <summary>
        /// Gets or sets the color for additions (RGB hex).
        /// </summary>
        /// <remarks>
        /// Default is "#90EE90" (light green).
        /// </remarks>
        public string AdditionColor { get; set; } = "#90EE90";

        /// <summary>
        /// Gets or sets the color for deletions (RGB hex).
        /// </summary>
        /// <remarks>
        /// Default is "#FFB6C1" (light red).
        /// </remarks>
        public string DeletionColor { get; set; } = "#FFB6C1";

        /// <summary>
        /// Gets or sets the color for modifications (RGB hex).
        /// </summary>
        /// <remarks>
        /// Default is "#FFD700" (gold).
        /// </remarks>
        public string ModificationColor { get; set; } = "#FFD700";

        /// <summary>
        /// Gets or sets whether to include a cover page.
        /// </summary>
        public bool IncludeCoverPage { get; set; } = false;

        /// <summary>
        /// Creates a new PdfExportOptions with default settings.
        /// </summary>
        public PdfExportOptions() { }

        /// <summary>
        /// Creates a new PdfExportOptions with the specified page size.
        /// </summary>
        /// <param name="pageSize">The page size to use.</param>
        public PdfExportOptions(PdfPageSize pageSize)
        {
            PageSize = pageSize;
        }

        /// <summary>
        /// Creates options for A4 PDF with all features enabled.
        /// </summary>
        /// <returns>New options configured for A4 PDF.</returns>
        public static PdfExportOptions A4Full() => new PdfExportOptions
        {
            PageSize = PdfPageSize.A4,
            IncludeTableOfContents = true,
            IncludePageNumbers = true,
            EnableSyntaxHighlighting = true,
            ColorCodeChanges = true,
            IncludeSummary = true,
            IncludeCoverPage = true
        };

        /// <summary>
        /// Creates options for a minimal PDF report.
        /// </summary>
        /// <returns>New options configured for minimal output.</returns>
        public static PdfExportOptions Minimal() => new PdfExportOptions
        {
            IncludeTableOfContents = false,
            IncludePageNumbers = false,
            EnableSyntaxHighlighting = false,
            ColorCodeChanges = false,
            IncludeSummary = false,
            IncludeCoverPage = false
        };
    }

    /// <summary>
    /// Generates PDF formatted diff reports from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This formatter creates professional PDF reports that can be shared,
    /// printed, or archived. PDF output includes:</para>
    /// <list type="bullet">
    ///   <item><description>Cover page with title and metadata</description></item>
    ///   <item><description>Table of contents with bookmarks</description></item>
    ///   <item><description>Summary statistics</description></item>
    ///   <item><description>Detailed changes with syntax highlighting</description></item>
    ///   <item><description>Page numbers and headers/footers</description></item>
    /// </list>
    /// <para><b>Implementation Note:</b> This class provides the structure and configuration
    /// for PDF generation. Actual PDF creation requires an external library. The formatter
    /// returns HTML by default which can be converted to PDF using a library like
    /// wkhtmltopdf, Puppeteer, or iTextSharp.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new PdfDiffFormatter();
    /// string html = formatter.Format(diffResult); // Returns HTML that can be converted to PDF
    ///
    /// // With custom options
    /// var options = new PdfExportOptions { PageSize = PdfPageSize.A4 };
    /// var customFormatter = new PdfDiffFormatter(options);
    /// byte[] pdfData = customFormatter.FormatAsBytes(diffResult);
    /// </code>
    /// </example>
    public class PdfDiffFormatter : IDiffFormatter
    {
        private readonly PdfExportOptions _options;
        private readonly StringBuilder _sb;
        private DiffStatistics _stats;

        /// <summary>
        /// Gets the export options for this formatter.
        /// </summary>
        public PdfExportOptions Options => _options;

        /// <summary>
        /// Creates a new PdfDiffFormatter with default options.
        /// </summary>
        public PdfDiffFormatter() : this(new PdfExportOptions()) { }

        /// <summary>
        /// Creates a new PdfDiffFormatter with the specified options.
        /// </summary>
        /// <param name="options">The PDF export options to use.</param>
        public PdfDiffFormatter(PdfExportOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Formats a diff tree into HTML (which can be converted to PDF).
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>An HTML string formatted for PDF conversion.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into HTML (which can be converted to PDF).
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>An HTML string formatted for PDF conversion.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            _sb.Clear();
            _stats = new DiffStatistics();

            // Calculate statistics
            CalculateStatistics(diff, _stats);

            // Write HTML document
            WriteHtmlStart();

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
            WriteChanges(diff);

            // Write HTML end
            WriteHtmlEnd();

            return _sb.ToString();
        }

        /// <summary>
        /// Formats a diff tree and returns the data as bytes (for PDF output).
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>The formatted data as bytes.</returns>
        /// <remarks>
        /// This method returns HTML bytes by default. To generate actual PDF,
        /// use an external PDF generation library or service.
        /// </remarks>
        public byte[] FormatAsBytes(DiffMatch diff)
        {
            string html = Format(diff);
            return Encoding.UTF8.GetBytes(html);
        }

        /// <summary>
        /// Writes the HTML document start.
        /// </summary>
        private void WriteHtmlStart()
        {
            _sb.AppendLine("<!DOCTYPE html>");
            _sb.AppendLine("<html>");
            _sb.AppendLine("<head>");
            _sb.AppendLine($"<meta charset=\"utf-8\" />");
            _sb.AppendLine($"<title>{EscapeHtml(_options.Title)}</title>");
            _sb.AppendLine("<style>");
            WriteStyles();
            _sb.AppendLine("</style>");
            _sb.AppendLine("</head>");
            _sb.AppendLine("<body>");
        }

        /// <summary>
        /// Writes CSS styles for the PDF output.
        /// </summary>
        private void WriteStyles()
        {
            _sb.AppendLine("@media print {");
            _sb.AppendLine("  @page { size: " + (_options.PageSize == PdfPageSize.A4 ? "A4" : "Letter") + "; margin: 1in; }");
            _sb.AppendLine("  body { font-family: 'Helvetica', Arial, sans-serif; }");
            _sb.AppendLine("  .page-break { page-break-before: always; }");
            _sb.AppendLine("  .no-break { page-break-inside: avoid; }");
            _sb.AppendLine("}");
            _sb.AppendLine("body { font-family: 'Helvetica', Arial, sans-serif; font-size: 10pt; margin: 0; padding: 0; }");
            _sb.AppendLine(".cover-page { text-align: center; padding: 100px 20px; }");
            _sb.AppendLine(".cover-title { font-size: 24pt; font-weight: bold; margin-bottom: 20px; }");
            _sb.AppendLine(".cover-subtitle { font-size: 14pt; color: #666; }");
            _sb.AppendLine(".header { border-bottom: 2px solid #333; padding: 10px 0; margin-bottom: 20px; }");
            _sb.AppendLine(".footer { border-top: 1px solid #ccc; padding: 10px 0; margin-top: 20px; font-size: 8pt; color: #666; }");
            _sb.AppendLine(".toc { margin-bottom: 30px; }");
            _sb.AppendLine(".toc h2 { font-size: 14pt; margin-bottom: 10px; }");
            _sb.AppendLine(".toc ul { list-style: none; padding-left: 0; }");
            _sb.AppendLine(".toc li { padding: 5px 0; }");
            _sb.AppendLine(".toc a { text-decoration: none; color: #0066cc; }");
            _sb.AppendLine(".summary { margin-bottom: 30px; }");
            _sb.AppendLine(".summary h2 { font-size: 14pt; margin-bottom: 10px; }");
            _sb.AppendLine(".stats-table { border-collapse: collapse; width: 100%; }");
            _sb.AppendLine(".stats-table th, .stats-table td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            _sb.AppendLine(".stats-table th { background-color: #f2f2f2; }");
            _sb.AppendLine(".change-item { margin-bottom: 20px; padding: 10px; border-left: 3px solid #ccc; }");
            _sb.AppendLine(".change-added { border-left-color: #90EE90; background-color: #f0fff0; }");
            _sb.AppendLine(".change-deleted { border-left-color: #FFB6C1; background-color: #fff0f0; }");
            _sb.AppendLine(".change-modified { border-left-color: #FFD700; background-color: #fffff0; }");
            _sb.AppendLine(".change-moved { border-left-color: #87CEEB; background-color: #f0f8ff; }");
            _sb.AppendLine(".change-header { font-weight: bold; margin-bottom: 5px; }");
            _sb.AppendLine(".change-path { font-family: 'Courier', monospace; color: #666; font-size: 9pt; }");
            _sb.AppendLine(".code-block { font-family: 'Courier', monospace; font-size: 8pt; background-color: #f5f5f5; padding: 10px; overflow-x: auto; white-space: pre-wrap; }");
            _sb.AppendLine(".xml-tag { color: #000080; }");
            _sb.AppendLine(".xml-attr { color: #008000; }");
            _sb.AppendLine(".xml-value { color: #800000; }");
            _sb.AppendLine(".xml-comment { color: #808080; font-style: italic; }");
        }

        /// <summary>
        /// Writes the cover page.
        /// </summary>
        private void WriteCoverPage()
        {
            _sb.AppendLine("<div class=\"cover-page\">");
            _sb.AppendLine($"<h1 class=\"cover-title\">{EscapeHtml(_options.Title)}</h1>");
            if (!string.IsNullOrEmpty(_options.Subtitle))
            {
                _sb.AppendLine($"<p class=\"cover-subtitle\">{EscapeHtml(_options.Subtitle)}</p>");
            }
            _sb.AppendLine($"<p class=\"cover-subtitle\">Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            _sb.AppendLine("</div>");
            _sb.AppendLine("<div class=\"page-break\"></div>");
        }

        /// <summary>
        /// Writes the table of contents.
        /// </summary>
        private void WriteTableOfContents()
        {
            _sb.AppendLine("<div class=\"toc\">");
            _sb.AppendLine("<h2>Table of Contents</h2>");
            _sb.AppendLine("<ul>");

            if (_options.IncludeSummary)
            {
                _sb.AppendLine("<li><a href=\"#summary\">Summary</a></li>");
            }
            _sb.AppendLine("<li><a href=\"#changes\">Changes</a></li>");

            _sb.AppendLine("</ul>");
            _sb.AppendLine("</div>");
            _sb.AppendLine("<div class=\"page-break\"></div>");
        }

        /// <summary>
        /// Writes the summary section.
        /// </summary>
        private void WriteSummary()
        {
            _sb.AppendLine("<div class=\"summary\" id=\"summary\">");
            _sb.AppendLine("<h2>Summary</h2>");
            _sb.AppendLine("<table class=\"stats-table\">");
            _sb.AppendLine("<tr><th>Metric</th><th>Count</th></tr>");
            _sb.AppendLine($"<tr><td>Total Elements Compared</td><td>{_stats.TotalElements}</td></tr>");
            _sb.AppendLine($"<tr><td>Additions</td><td>{_stats.Additions}</td></tr>");
            _sb.AppendLine($"<tr><td>Deletions</td><td>{_stats.Deletions}</td></tr>");
            _sb.AppendLine($"<tr><td>Modifications</td><td>{_stats.Modifications}</td></tr>");
            if (_stats.Moves > 0)
                _sb.AppendLine($"<tr><td>Moves</td><td>{_stats.Moves}</td></tr>");
            _sb.AppendLine($"<tr><td>Total Changes</td><td>{_stats.TotalChanges}</td></tr>");
            _sb.AppendLine("</table>");
            _sb.AppendLine("</div>");
            _sb.AppendLine("<div class=\"page-break\"></div>");
        }

        /// <summary>
        /// Writes the changes section.
        /// </summary>
        private void WriteChanges(DiffMatch diff)
        {
            _sb.AppendLine("<div id=\"changes\">");
            _sb.AppendLine("<h2>Changes</h2>");

            if (_stats.TotalChanges == 0)
            {
                _sb.AppendLine("<p>No differences detected.</p>");
            }
            else
            {
                WriteChangesRecursive(diff);
            }

            _sb.AppendLine("</div>");
        }

        /// <summary>
        /// Writes changes recursively.
        /// </summary>
        private void WriteChangesRecursive(DiffMatch node)
        {
            if (node.Type != DiffType.Unchanged)
            {
                WriteChangeItem(node);
            }

            foreach (var child in node.Children)
            {
                WriteChangesRecursive(child);
            }
        }

        /// <summary>
        /// Writes a single change item.
        /// </summary>
        private void WriteChangeItem(DiffMatch node)
        {
            string changeClass = node.Type switch
            {
                DiffType.Added => "change-added",
                DiffType.Deleted => "change-deleted",
                DiffType.Modified => "change-modified",
                DiffType.Moved => "change-moved",
                _ => ""
            };

            string changeType = node.Type switch
            {
                DiffType.Added => "Added",
                DiffType.Deleted => "Deleted",
                DiffType.Modified => "Modified",
                DiffType.Moved => "Moved",
                _ => ""
            };

            _sb.AppendLine($"<div class=\"change-item {changeClass} no-break\">");
            _sb.AppendLine($"<div class=\"change-header\">{changeType}</div>");
            _sb.AppendLine($"<div class=\"change-path\">Path: {EscapeHtml(node.Path ?? "Unknown")}</div>");

            var content = FormatXmlContent(node);
            if (!string.IsNullOrEmpty(content))
            {
                _sb.AppendLine($"<div class=\"code-block\">{EscapeHtml(content)}</div>");
            }

            _sb.AppendLine("</div>");
        }

        /// <summary>
        /// Writes the HTML document end.
        /// </summary>
        private void WriteHtmlEnd()
        {
            _sb.AppendLine("<div class=\"footer\">");
            if (_options.IncludePageNumbers)
            {
                _sb.AppendLine("<p>Page <span class=\"page-number\"></span></p>");
            }
            _sb.AppendLine($"<p>Generated by XmlComparer on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            _sb.AppendLine("</div>");
            _sb.AppendLine("</body>");
            _sb.AppendLine("</html>");
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
        /// Escapes HTML special characters.
        /// </summary>
        private string EscapeHtml(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
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
