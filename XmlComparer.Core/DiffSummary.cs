namespace XmlComparer.Core
{
    /// <summary>
    /// Contains summary statistics for a diff tree, showing counts of each type of change.
    /// </summary>
    /// <remarks>
    /// <para>This class is returned by <see cref="DiffSummaryCalculator.Compute(DiffMatch)"/> and provides
    /// a quick overview of changes without needing to traverse the entire diff tree.</para>
    /// <para>The summary includes counts for all diff types: <see cref="Unchanged"/>, <see cref="Modified"/>,
    /// <see cref="Added"/>, <see cref="Deleted"/>, and <see cref="Moved"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
    /// var summary = DiffSummaryCalculator.Compute(diff);
    ///
    /// Console.WriteLine($"Total nodes: {summary.Total}");
    /// Console.WriteLine($"Unchanged: {summary.Unchanged}");
    /// Console.WriteLine($"Modified: {summary.Modified}");
    /// Console.WriteLine($"Added: {summary.Added}");
    /// Console.WriteLine($"Deleted: {summary.Deleted}");
    /// Console.WriteLine($"Moved: {summary.Moved}");
    ///
    /// // Check if files are identical
    /// if (summary.Modified == 0 && summary.Added == 0 && summary.Deleted == 0)
    ///     Console.WriteLine("Files are identical!");
    /// </code>
    /// </example>
    /// <seealso cref="DiffSummaryCalculator"/>
    public class DiffSummary
    {
        /// <summary>
        /// Gets or sets the total number of nodes in the diff tree.
        /// </summary>
        /// <remarks>
        /// This count includes all nodes regardless of their diff type.
        /// </remarks>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes that were unchanged between documents.
        /// </summary>
        /// <remarks>
        /// Unchanged nodes have identical content, attributes, and children in both documents.
        /// </remarks>
        public int Unchanged { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes that were added to the new document.
        /// </summary>
        /// <remarks>
        /// Added nodes exist only in the new document and not in the original.
        /// </remarks>
        public int Added { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes that were deleted from the original document.
        /// </summary>
        /// <remarks>
        /// Deleted nodes exist only in the original document and not in the new document.
        /// </remarks>
        public int Deleted { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes that were modified between documents.
        /// </summary>
        /// <remarks>
        /// Modified nodes exist in both documents but have different attributes,
        /// text content, or child structure.
        /// </remarks>
        public int Modified { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes that were moved to different positions.
        /// </summary>
        /// <remarks>
        /// Moved nodes exist in both documents at different positions. Move detection
        /// requires key attributes to be configured via <see cref="XmlComparisonOptions.WithKeyAttributes(string[])"/>.
        /// </remarks>
        public int Moved { get; set; }
    }
}
