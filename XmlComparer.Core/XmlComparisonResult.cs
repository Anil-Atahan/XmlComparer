namespace XmlComparer.Core
{
    /// <summary>
    /// Encapsulates the complete results of an XML comparison operation.
    /// </summary>
    /// <remarks>
    /// <para>This class provides a unified container for all outputs generated during comparison:
    /// the diff tree, optional validation results, and generated reports in HTML and/or JSON formats.</para>
    /// <para>Access the individual components through the readonly properties:
    /// <see cref="Diff"/> for the comparison tree,
    /// <see cref="Validation"/> for XSD validation results (if performed),
    /// <see cref="Html"/> for the HTML report (if generated),
    /// and <see cref="Json"/> for the JSON report (if generated).</para>
    /// </remarks>
    /// <example>
    /// This example shows how to use the result object:
    /// <code>
    /// var result = XmlComparer.CompareFilesWithReport("old.xml", "new.xml",
    ///     options => options.IncludeHtml().IncludeJson());
    ///
    /// // Access the diff tree
    /// var summary = DiffSummaryCalculator.Compute(result.Diff);
    /// Console.WriteLine($"Changes: {summary.Modified} modified, {summary.Added} added, {summary.Deleted} deleted");
    ///
    /// // Check validation results
    /// if (result.Validation != null &amp;&amp; !result.Validation.IsValid)
    /// {
    ///     Console.WriteLine("Validation failed:");
    ///     foreach (var error in result.Validation.Errors)
    ///         Console.WriteLine($"  - {error.Message}");
    /// }
    ///
    /// // Save reports
    /// if (result.Html != null)
    ///     File.WriteAllText("diff.html", result.Html);
    /// if (result.Json != null)
    ///     File.WriteAllText("diff.json", result.Json);
    /// </code>
    /// </example>
    /// <seealso cref="XmlComparer"/>
    /// <seealso cref="DiffMatch"/>
    /// <seealso cref="XmlValidationResult"/>
    public class XmlComparisonResult
    {
        /// <summary>
        /// Creates a new comparison result with the specified outputs.
        /// </summary>
        /// <param name="diff">The root of the diff tree representing all changes.</param>
        /// <param name="validation">The XSD validation results, or null if validation was not performed.</param>
        /// <param name="html">The HTML report, or null if HTML was not generated.</param>
        /// <param name="json">The JSON report, or null if JSON was not generated.</param>
        public XmlComparisonResult(DiffMatch diff, XmlValidationResult? validation, string? html, string? json)
        {
            Diff = diff;
            Validation = validation;
            Html = html;
            Json = json;
        }

        /// <summary>
        /// Gets the root of the diff tree.
        /// </summary>
        /// <remarks>
        /// The diff tree is a hierarchical structure where each <see cref="DiffMatch"/> node
        /// represents an XML element and its changes. Traverse <see cref="DiffMatch.Children"/> recursively
        /// to examine all changes in the document.
        /// </remarks>
        /// <example>
        /// <code>
        /// void PrintChanges(DiffMatch node, int indent = 0)
        /// {
        ///     if (node.Type != DiffType.Unchanged)
        ///     {
        ///         Console.WriteLine($"{new string(' ', indent)}{node.Type}: {node.Path}");
        ///     }
        ///     foreach (var child in node.Children)
        ///         PrintChanges(child, indent + 2);
        /// }
        ///
        /// PrintChanges(result.Diff);
        /// </code>
        /// </example>
        public DiffMatch Diff { get; }

        /// <summary>
        /// Gets the XSD validation results, or null if validation was not performed.
        /// </summary>
        /// <remarks>
        /// Validation is only performed when XSD schemas are specified via
        /// <see cref="XmlComparisonOptions.ValidateWithXsds(string[])"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (result.Validation != null)
        /// {
        ///     if (result.Validation.IsValid)
        ///         Console.WriteLine("Both XML files are valid according to the schema.");
        ///     else
        ///     {
        ///         Console.WriteLine("Validation errors found:");
        ///         foreach (var error in result.Validation.Errors)
        ///             Console.WriteLine($"  Line {error.LineNumber}: {error.Message}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public XmlValidationResult? Validation { get; }

        /// <summary>
        /// Gets the generated HTML report, or null if HTML was not requested.
        /// </summary>
        /// <remarks>
        /// The HTML report provides a side-by-side visual comparison of the XML files
        /// with color-coded changes. HTML generation is enabled via
        /// <see cref="XmlComparisonOptions.IncludeHtml(bool, bool)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (result.Html != null)
        /// {
        ///     File.WriteAllText("comparison.html", result.Html);
        ///     Console.WriteLine("HTML report saved to comparison.html");
        /// }
        /// </code>
        /// </example>
        public string? Html { get; }

        /// <summary>
        /// Gets the generated JSON report, or null if JSON was not requested.
        /// </summary>
        /// <remarks>
        /// The JSON report provides a machine-readable representation of the diff,
        /// suitable for automated processing or API responses. JSON generation is enabled via
        /// <see cref="XmlComparisonOptions.IncludeJson(bool, bool)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (result.Json != null)
        /// {
        ///     File.WriteAllText("comparison.json", result.Json);
        ///     Console.WriteLine("JSON report saved to comparison.json");
        /// }
        /// </code>
        /// </example>
        public string? Json { get; }
    }
}
