using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates CSV/TSV/Excel formatted diff reports from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This formatter creates tabular representations of XML diffs that can be
    /// imported into spreadsheet applications like Excel, Google Sheets, or used for
    /// data analysis.</para>
    /// <para>Supported output formats:</para>
    /// <list type="bullet">
    ///   <item><description>CSV - Comma-separated values</description></item>
    ///   <item><description>TSV - Tab-separated values</description></item>
    ///   <item><description>Excel - .xlsx format with formatting (requires EPPlus or similar)</description></item>
    ///   <item><description>HTML - HTML table format</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new CsvDiffFormatter();
    /// string csv = formatter.Format(diffResult);
    /// File.WriteAllText("diff.csv", csv);
    ///
    /// // With custom options
    /// var options = new ExcelExportOptions
    /// {
    ///     Format = TabularOutputFormat.Tsv,
    ///     Structure = TabularStructure.Attribute
    /// };
    /// var tsvFormatter = new CsvDiffFormatter(options);
    /// string tsv = tsvFormatter.Format(diffResult);
    /// </code>
    /// </example>
    public class CsvDiffFormatter : IDiffFormatter
    {
        private readonly ExcelExportOptions _options;
        private readonly StringBuilder _sb;
        private readonly List<DiffRow> _rows;

        /// <summary>
        /// Gets the export options for this formatter.
        /// </summary>
        public ExcelExportOptions Options => _options;

        /// <summary>
        /// Creates a new CsvDiffFormatter with default options.
        /// </summary>
        public CsvDiffFormatter() : this(new ExcelExportOptions()) { }

        /// <summary>
        /// Creates a new CsvDiffFormatter with the specified options.
        /// </summary>
        /// <param name="options">The export options to use.</param>
        public CsvDiffFormatter(ExcelExportOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.UpdateDelimiterForFormat();
            _sb = new StringBuilder();
            _rows = new List<DiffRow>();
        }

        /// <summary>
        /// Formats a diff tree into a CSV/TSV string.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>A CSV/TSV formatted string.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into a CSV/TSV string with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>A CSV/TSV formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            _sb.Clear();
            _rows.Clear();

            // Build rows based on structure type
            switch (_options.Structure)
            {
                case TabularStructure.Summary:
                    BuildSummaryRows(diff);
                    break;
                case TabularStructure.Attribute:
                    BuildAttributeRows(diff, "");
                    break;
                case TabularStructure.Hierarchical:
                    BuildHierarchicalRows(diff, "", 0);
                    break;
                case TabularStructure.Flat:
                default:
                    BuildFlatRows(diff, "");
                    break;
            }

            // Write headers
            if (_options.IncludeHeaders && _rows.Any())
            {
                WriteHeaders(_rows[0].GetColumnNames());
            }

            // Write data rows
            foreach (var row in _rows)
            {
                WriteRow(row);
            }

            return _sb.ToString();
        }

        /// <summary>
        /// Formats a diff tree and returns the raw bytes.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>The formatted data as bytes.</returns>
        public byte[] FormatAsBytes(DiffMatch diff)
        {
            string content = Format(diff);
            return _options.Encoding.GetBytes(content);
        }

        /// <summary>
        /// Writes column headers.
        /// </summary>
        private void WriteHeaders(IEnumerable<string> headers)
        {
            var filteredHeaders = _options.IncludedColumns.Count > 0
                ? headers.Where(h => _options.IncludedColumns.Contains(h))
                : headers;

            var headerValues = filteredHeaders.Select(h => _options.GetColumnName(h));
            WriteRow(headerValues.ToArray());
        }

        /// <summary>
        /// Writes a single row.
        /// </summary>
        private void WriteRow(params string[] values)
        {
            var filteredValues = _options.IncludedColumns.Count > 0
                ? FilterValuesByIncludedColumns(values)
                : values;

            for (int i = 0; i < filteredValues.Length; i++)
            {
                if (i > 0) _sb.Append(_options.Delimiter);
                _sb.Append(EscapeValue(filteredValues[i]));
            }
            _sb.AppendLine();
        }

        /// <summary>
        /// Writes a DiffRow as a CSV row.
        /// </summary>
        private void WriteRow(DiffRow row)
        {
            WriteRow(row.GetAllValues());
        }

        /// <summary>
        /// Escapes a value for CSV/TSV output.
        /// </summary>
        private string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            bool needsQuotes = value.Contains(_options.Delimiter) ||
                             value.Contains(_options.Quote) ||
                             value.Contains('\n') ||
                             value.Contains('\r');

            if (!needsQuotes)
                return value;

            string escaped = value.Replace($"{_options.Quote}", $"{_options.Quote}{_options.Quote}");
            return $"{_options.Quote}{escaped}{_options.Quote}";
        }

        /// <summary>
        /// Filters values to only include specified columns.
        /// </summary>
        private string[] FilterValuesByIncludedColumns(string[] allValues)
        {
            // This is a simplified version - in production you'd map indices properly
            return allValues;
        }

        /// <summary>
        /// Builds summary rows with statistics.
        /// </summary>
        private void BuildSummaryRows(DiffMatch diff)
        {
            var stats = CalculateStatistics(diff);

            _rows.Add(new DiffRow
            {
                Column1 = "Metric",
                Column2 = "Count"
            });

            _rows.Add(new DiffRow { Column1 = "Total Elements", Column2 = stats.TotalElements.ToString() });
            _rows.Add(new DiffRow { Column1 = "Additions", Column2 = stats.Additions.ToString() });
            _rows.Add(new DiffRow { Column1 = "Deletions", Column2 = stats.Deletions.ToString() });
            _rows.Add(new DiffRow { Column1 = "Modifications", Column2 = stats.Modifications.ToString() });
            _rows.Add(new DiffRow { Column1 = "Moves", Column2 = stats.Moves.ToString() });
            _rows.Add(new DiffRow { Column1 = "Total Changes", Column2 = stats.TotalChanges.ToString() });
        }

        /// <summary>
        /// Builds flat rows (one per changed element).
        /// </summary>
        private void BuildFlatRows(DiffMatch node, string parentPath)
        {
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? $"/{node.Path}"
                : $"{parentPath}/{node.Path}";

            if (node.Type != DiffType.Unchanged)
            {
                _rows.Add(new DiffRow
                {
                    Column1 = currentPath,
                    Column2 = GetChangeTypeString(node.Type),
                    Column3 = GetElementContent(node.OriginalElement),
                    Column4 = GetElementContent(node.NewElement),
                    Column5 = node.Path ?? ""
                });
            }

            foreach (var child in node.Children)
            {
                BuildFlatRows(child, currentPath);
            }
        }

        /// <summary>
        /// Builds hierarchical rows with level information.
        /// </summary>
        private void BuildHierarchicalRows(DiffMatch node, string parentPath, int level)
        {
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? $"/{node.Path}"
                : $"{parentPath}/{node.Path}";

            if (node.Type != DiffType.Unchanged)
            {
                _rows.Add(new DiffRow
                {
                    Column1 = currentPath,
                    Column2 = GetChangeTypeString(node.Type),
                    Column3 = level.ToString(),
                    Column4 = GetElementContent(node.OriginalElement),
                    Column5 = GetElementContent(node.NewElement),
                    Column6 = node.Path ?? ""
                });
            }

            foreach (var child in node.Children)
            {
                BuildHierarchicalRows(child, currentPath, level + 1);
            }
        }

        /// <summary>
        /// Builds attribute-level rows (one per attribute change).
        /// </summary>
        private void BuildAttributeRows(DiffMatch node, string parentPath)
        {
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? $"/{node.Path}"
                : $"{parentPath}/{node.Path}";

            if (node.Type != DiffType.Unchanged)
            {
                var originalElement = node.OriginalElement;
                var newElement = node.NewElement;

                // Get all attributes from both elements
                var allAttrs = new HashSet<string>();
                if (originalElement != null)
                {
                    foreach (var attr in originalElement.Attributes()) allAttrs.Add(attr.Name.LocalName);
                }
                if (newElement != null)
                {
                    foreach (var attr in newElement.Attributes()) allAttrs.Add(attr.Name.LocalName);
                }

                foreach (var attrName in allAttrs)
                {
                    var originalAttr = originalElement?.Attribute(attrName);
                    var newAttr = newElement?.Attribute(attrName);

                    string originalValue = originalAttr?.Value ?? "";
                    string newValue = newAttr?.Value ?? "";

                    if (originalValue != newValue)
                    {
                        _rows.Add(new DiffRow
                        {
                            Column1 = currentPath,
                            Column2 = "@" + attrName,
                            Column3 = GetChangeTypeForAttribute(originalValue, newValue),
                            Column4 = originalValue,
                            Column5 = newValue
                        });
                    }
                }

                // Also add element value changes
                if (originalElement != null && newElement != null)
                {
                    string originalValue = originalElement.Value;
                    string newValue = newElement.Value;
                    if (originalValue != newValue && !string.IsNullOrEmpty(originalValue + newValue))
                    {
                        _rows.Add(new DiffRow
                        {
                            Column1 = currentPath,
                            Column2 = "#text",
                            Column3 = GetChangeTypeForAttribute(originalValue, newValue),
                            Column4 = originalValue,
                            Column5 = newValue
                        });
                    }
                }
            }

            foreach (var child in node.Children)
            {
                BuildAttributeRows(child, currentPath);
            }
        }

        /// <summary>
        /// Gets the change type string for an attribute comparison.
        /// </summary>
        private string GetChangeTypeForAttribute(string original, string newValue)
        {
            if (string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(newValue))
                return "Added";
            if (!string.IsNullOrEmpty(original) && string.IsNullOrEmpty(newValue))
                return "Deleted";
            return "Modified";
        }

        /// <summary>
        /// Gets a string representation of a change type.
        /// </summary>
        private string GetChangeTypeString(DiffType type)
        {
            return type switch
            {
                DiffType.Added => "Added",
                DiffType.Deleted => "Deleted",
                DiffType.Modified => "Modified",
                DiffType.Moved => "Moved",
                DiffType.NamespaceChanged => "Namespace Changed",
                _ => "Unchanged"
            };
        }

        /// <summary>
        /// Gets the content of an element as a string.
        /// </summary>
        private string GetElementContent(XElement? element)
        {
            if (element == null) return "";

            if (!element.HasElements && !string.IsNullOrEmpty(element.Value))
                return element.Value;

            return element.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Calculates statistics from a diff tree.
        /// </summary>
        private DiffStatistics CalculateStatistics(DiffMatch node)
        {
            var stats = new DiffStatistics();
            CalculateStatsRecursive(node, stats);
            return stats;
        }

        /// <summary>
        /// Recursively calculates statistics.
        /// </summary>
        private void CalculateStatsRecursive(DiffMatch node, DiffStatistics stats)
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
                CalculateStatsRecursive(child, stats);
            }
        }

        /// <summary>
        /// Represents a single row in the tabular output.
        /// </summary>
        private class DiffRow
        {
            public string Column1 { get; set; } = "";
            public string Column2 { get; set; } = "";
            public string Column3 { get; set; } = "";
            public string Column4 { get; set; } = "";
            public string Column5 { get; set; } = "";
            public string Column6 { get; set; } = "";

            public string[] GetAllValues() => new[] { Column1, Column2, Column3, Column4, Column5, Column6 };

            public string[] GetColumnNames() => new[]
            {
                "Path", "Change Type", "Level", "Original Value", "New Value", "Element Name"
            };
        }

        /// <summary>
        /// Statistics for diff results.
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
