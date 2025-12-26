using System.Collections.Generic;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines the output format for tabular diff export.
    /// </summary>
    public enum TabularOutputFormat
    {
        /// <summary>
        /// Comma-Separated Values format.
        /// </summary>
        Csv,

        /// <summary>
        /// Tab-Separated Values format.
        /// </summary>
        Tsv,

        /// <summary>
        /// Microsoft Excel (.xlsx) format.
        /// </summary>
        Excel,

        /// <summary>
        /// HTML table format.
        /// </summary>
        Html
    }

    /// <summary>
    /// Defines how to structure the tabular output.
    /// </summary>
    public enum TabularStructure
    {
        /// <summary>
        /// One row per changed element with columns for path, type, old value, new value.
        /// </summary>
        Flat,

        /// <summary>
        /// Hierarchical structure with parent-child relationships.
        /// </summary>
        Hierarchical,

        /// <summary>
        /// Attribute-level granularity (one row per attribute change).
        /// </summary>
        Attribute,

        /// <summary>
        /// Summary statistics only.
        /// </summary>
        Summary
    }

    /// <summary>
    /// Configuration options for Excel and tabular diff export.
    /// </summary>
    /// <remarks>
    /// <para>This class controls how XML diffs are exported to tabular formats
    /// such as CSV, TSV, Excel, or HTML tables.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new ExcelExportOptions
    /// {
    ///     Format = TabularOutputFormat.Excel,
    ///     Structure = TabularStructure.Flat,
    ///     IncludeHeaders = true,
    ///     ApplyConditionalFormatting = true
    /// };
    ///
    /// var formatter = new CsvDiffFormatter(options);
    /// byte[] excelData = formatter.FormatAsBytes(diffResult);
    /// File.WriteAllBytes("diff.xlsx", excelData);
    /// </code>
    /// </example>
    public class ExcelExportOptions
    {
        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="TabularOutputFormat.Csv"/>.
        /// </remarks>
        public TabularOutputFormat Format { get; set; } = TabularOutputFormat.Csv;

        /// <summary>
        /// Gets or sets the structure type for the output.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="TabularStructure.Flat"/>.
        /// </remarks>
        public TabularStructure Structure { get; set; } = TabularStructure.Flat;

        /// <summary>
        /// Gets or sets whether to include column headers.
        /// </summary>
        /// <remarks>
        /// When true, the first row contains column names.
        /// </remarks>
        public bool IncludeHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the delimiter character for CSV/TSV output.
        /// </summary>
        /// <remarks>
        /// Default is comma for CSV, tab for TSV. This property is automatically
        /// set based on the Format, but can be overridden.
        /// </remarks>
        public char Delimiter { get; set; } = ',';

        /// <summary>
        /// Gets or sets the quote character for CSV/TSV output.
        /// </summary>
        /// <remarks>
        /// Default is double quote (").
        /// </remarks>
        public char Quote { get; set; } = '"';

        /// <summary>
        /// Gets or sets whether to apply conditional formatting in Excel.
        /// </summary>
        /// <remarks>
        /// When true, applies color coding: green for additions, red for deletions,
        /// yellow for modifications.
        /// </remarks>
        public bool ApplyConditionalFormatting { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to freeze the header row in Excel.
        /// </summary>
        /// <remarks>
        /// When true, the header row stays visible when scrolling.
        /// </remarks>
        public bool FreezeHeaderRow { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to auto-fit column widths in Excel.
        /// </summary>
        /// <remarks>
        /// When true, columns are sized to fit their content.
        /// </remarks>
        public bool AutoFitColumns { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include a summary sheet in Excel.
        /// </summary>
        /// <remarks>
        /// When true, creates an additional sheet with statistics.
        /// </remarks>
        public bool IncludeSummarySheet { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the main worksheet.
        /// </summary>
        /// <remarks>
        /// Default is "Changes".
        /// </remarks>
        public string WorksheetName { get; set; } = "Changes";

        /// <summary>
        /// Gets or sets the name of the summary worksheet.
        /// </summary>
        /// <remarks>
        /// Default is "Summary".
        /// </remarks>
        public string SummarySheetName { get; set; } = "Summary";

        /// <summary>
        /// Gets or sets the maximum number of rows per sheet in Excel.
        /// </summary>
        /// <remarks>
        /// Excel has a limit of 1,048,576 rows per sheet. When exceeded,
        /// additional sheets are created with numeric suffixes.
        /// </remarks>
        public int MaxRowsPerSheet { get; set; } = 1048576;

        /// <summary>
        /// Gets or sets custom column names for the output.
        /// </summary>
        /// <remarks>
        /// Use this to localize column headers or match specific schemas.
        /// The keys are default column names, values are custom names.
        /// </remarks>
        public Dictionary<string, string> CustomColumnNames { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets which columns to include in the output.
        /// </summary>
        /// <remarks>
        /// When null or empty, all columns are included.
        /// Use this to limit output to specific columns only.
        /// </remarks>
        public HashSet<string> IncludedColumns { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the encoding for text-based formats (CSV, TSV).
        /// </summary>
        /// <remarks>
        /// Default is UTF-8 with BOM for Excel compatibility.
        /// </remarks>
        public System.Text.Encoding Encoding { get; set; } = new System.Text.UTF8Encoding(true);

        /// <summary>
        /// Creates a new ExcelExportOptions with default CSV settings.
        /// </summary>
        public ExcelExportOptions()
        {
            UpdateDelimiterForFormat();
        }

        /// <summary>
        /// Creates a new ExcelExportOptions with the specified format.
        /// </summary>
        /// <param name="format">The output format.</param>
        public ExcelExportOptions(TabularOutputFormat format)
        {
            Format = format;
            UpdateDelimiterForFormat();
        }

        /// <summary>
        /// Updates the delimiter based on the current format.
        /// </summary>
        public void UpdateDelimiterForFormat()
        {
            Delimiter = Format switch
            {
                TabularOutputFormat.Tsv => '\t',
                _ => ','
            };
        }

        /// <summary>
        /// Gets the display name for a column.
        /// </summary>
        /// <param name="defaultName">The default column name.</param>
        /// <returns>The custom name if set, otherwise the default name.</returns>
        public string GetColumnName(string defaultName)
        {
            return CustomColumnNames.TryGetValue(defaultName, out var customName)
                ? customName
                : defaultName;
        }

        /// <summary>
        /// Creates options for CSV output with semicolon delimiter (Excel European).
        /// </summary>
        /// <returns>New options configured for European CSV format.</returns>
        public static ExcelExportOptions CsvEuropean() => new ExcelExportOptions
        {
            Format = TabularOutputFormat.Csv,
            Delimiter = ';',
            Encoding = new System.Text.UTF8Encoding(true)
        };

        /// <summary>
        /// Creates options for Excel output with all features enabled.
        /// </summary>
        /// <returns>New options configured for Excel with formatting.</returns>
        public static ExcelExportOptions ExcelFormatted() => new ExcelExportOptions
        {
            Format = TabularOutputFormat.Excel,
            ApplyConditionalFormatting = true,
            FreezeHeaderRow = true,
            AutoFitColumns = true,
            IncludeSummarySheet = true
        };

        /// <summary>
        /// Creates options for attribute-level granularity output.
        /// </summary>
        /// <returns>New options configured for attribute-level output.</returns>
        public static ExcelExportOptions AttributeLevel() => new ExcelExportOptions
        {
            Format = TabularOutputFormat.Csv,
            Structure = TabularStructure.Attribute,
            IncludeHeaders = true
        };

        /// <summary>
        /// Creates options for summary-only output.
        /// </summary>
        /// <returns>New options configured for summary statistics only.</returns>
        public static ExcelExportOptions SummaryOnly() => new ExcelExportOptions
        {
            Format = TabularOutputFormat.Csv,
            Structure = TabularStructure.Summary,
            IncludeHeaders = true
        };
    }
}
