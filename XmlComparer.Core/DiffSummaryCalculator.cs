namespace XmlComparer.Core
{
    /// <summary>
    /// Provides summary statistics for XML diff results.
    /// </summary>
    /// <remarks>
    /// Use this class to get counts of each type of change in a diff tree.
    /// The summary is useful for displaying overall change statistics.
    /// </remarks>
    /// <example>
    /// <code>
    /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
    /// var summary = DiffSummaryCalculator.Compute(diff);
    ///
    /// Console.WriteLine($"Total changes: {summary.Modified + summary.Added + summary.Deleted}");
    /// Console.WriteLine($"Added: {summary.Added}, Deleted: {summary.Deleted}");
    /// </code>
    /// </example>
    public static class DiffSummaryCalculator
    {
        /// <summary>
        /// Computes a summary of all node types in the diff tree.
        /// </summary>
        /// <param name="root">The root diff node to summarize.</param>
        /// <returns>A summary containing counts of each diff type.</returns>
        /// <example>
        /// <code>
        /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
        /// var summary = DiffSummaryCalculator.Compute(diff);
        ///
        /// // Display summary
        /// Console.WriteLine($"Total: {summary.Total}");
        /// Console.WriteLine($"Unchanged: {summary.Unchanged}");
        /// Console.WriteLine($"Modified: {summary.Modified}");
        /// Console.WriteLine($"Added: {summary.Added}");
        /// Console.WriteLine($"Deleted: {summary.Deleted}");
        /// Console.WriteLine($"Moved: {summary.Moved}");
        /// </code>
        /// </example>
        public static DiffSummary Compute(DiffMatch root)
        {
            var summary = new DiffSummary();
            Walk(root, summary);
            return summary;
        }

        /// <summary>
        /// Recursively walks the diff tree to count each type.
        /// </summary>
        private static void Walk(DiffMatch match, DiffSummary summary)
        {
            summary.Total++;
            switch (match.Type)
            {
                case DiffType.Unchanged:
                    summary.Unchanged++;
                    break;
                case DiffType.Added:
                    summary.Added++;
                    break;
                case DiffType.Deleted:
                    summary.Deleted++;
                    break;
                case DiffType.Modified:
                    summary.Modified++;
                    break;
                case DiffType.Moved:
                    summary.Moved++;
                    break;
            }

            foreach (var child in match.Children)
            {
                Walk(child, summary);
            }
        }
    }
}
