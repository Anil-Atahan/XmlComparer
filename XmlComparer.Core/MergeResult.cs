using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Represents the result of a three-way merge operation.
    /// </summary>
    /// <remarks>
    /// <para>The merge result contains:</para>
    /// <list type="bullet">
    ///   <item><description>The merged XML document (if successful or partially successful)</description></item>
    ///   <item><description>A list of any conflicts that occurred</description></item>
    ///   <item><description>Statistics about the merge operation</description></item>
    ///   <item><description>The overall success status</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await merger.MergeAsync(baseXml, oursXml, theirsXml);
    ///
    /// if (result.IsSuccess)
    /// {
    ///     Console.WriteLine("Merge completed successfully!");
    ///     File.WriteAllText("merged.xml", result.MergedDocument?.ToString());
    /// }
    /// else if (result.HasConflicts)
    /// {
    ///     Console.WriteLine($"Merge completed with {result.Conflicts.Count} conflicts:");
    ///     foreach (var conflict in result.Conflicts)
    ///     {
    ///         Console.WriteLine($"  - {conflict.Path}: {conflict.GetDescription()}");
    ///     }
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Merge failed: " + result.ErrorMessage);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ThreeWayMergeEngine"/>
    /// <seealso cref="MergeConflict"/>
    public class MergeResult
    {
        /// <summary>
        /// Gets or sets the merged XML document.
        /// </summary>
        /// <remarks>
        /// This contains the best-effort merge result, even if there were conflicts.
        /// Conflicted areas will be marked with conflict markers or will contain
        /// the resolution from the conflict resolver.
        /// </remarks>
        public XDocument? MergedDocument { get; set; }

        /// <summary>
        /// Gets or sets the list of conflicts that occurred during the merge.
        /// </summary>
        /// <remarks>
        /// An empty list indicates a clean merge with no conflicts.
        /// </remarks>
        public List<MergeConflict> Conflicts { get; set; } = new List<MergeConflict>();

        /// <summary>
        /// Gets or sets the merge statistics.
        /// </summary>
        public MergeStatistics Statistics { get; set; } = new MergeStatistics();

        /// <summary>
        /// Gets or sets an error message if the merge failed.
        /// </summary>
        /// <remarks>
        /// This is set when an unrecoverable error occurs during merging,
        /// such as a parsing error or null input.
        /// </remarks>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the exception that caused the merge to fail, if any.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets whether the merge completed successfully with no conflicts.
        /// </summary>
        public bool IsSuccess => Exception == null && !HasConflicts;

        /// <summary>
        /// Gets whether the merge completed but has unresolved conflicts.
        /// </summary>
        public bool HasConflicts => Conflicts.Count > 0;

        /// <summary>
        /// Gets whether the merge failed due to an error.
        /// </summary>
        public bool IsFailed => Exception != null;

        /// <summary>
        /// Gets the merged XML as a string.
        /// </summary>
        /// <returns>The XML document as a string, or null if the merge failed.</returns>
        public string? GetMergedXml()
        {
            return MergedDocument?.ToString();
        }

        /// <summary>
        /// Gets the merged XML as a string with formatting.
        /// </summary>
        /// <param name="saveOptions">The XML save options.</param>
        /// <returns>The formatted XML document as a string, or null if the merge failed.</returns>
        public string? GetMergedXml(SaveOptions saveOptions)
        {
            return MergedDocument?.ToString(saveOptions);
        }

        /// <summary>
        /// Creates a successful merge result with no conflicts.
        /// </summary>
        /// <param name="mergedDocument">The merged document.</param>
        /// <param name="statistics">The merge statistics.</param>
        /// <returns>A successful merge result.</returns>
        public static MergeResult Success(XDocument mergedDocument, MergeStatistics? statistics = null)
        {
            return new MergeResult
            {
                MergedDocument = mergedDocument,
                Statistics = statistics ?? new MergeStatistics()
            };
        }

        /// <summary>
        /// Creates a merge result with conflicts.
        /// </summary>
        /// <param name="mergedDocument">The best-effort merged document.</param>
        /// <param name="conflicts">The list of conflicts.</param>
        /// <param name="statistics">The merge statistics.</param>
        /// <returns>A merge result with conflicts.</returns>
        public static MergeResult WithConflicts(
            XDocument mergedDocument,
            List<MergeConflict> conflicts,
            MergeStatistics? statistics = null)
        {
            return new MergeResult
            {
                MergedDocument = mergedDocument,
                Conflicts = conflicts,
                Statistics = statistics ?? new MergeStatistics()
            };
        }

        /// <summary>
        /// Creates a failed merge result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <returns>A failed merge result.</returns>
        public static MergeResult Failure(string errorMessage, Exception? exception = null)
        {
            return new MergeResult
            {
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Statistics for a three-way merge operation.
    /// </summary>
    public class MergeStatistics
    {
        /// <summary>
        /// Gets or sets the total number of elements processed.
        /// </summary>
        public int TotalElements { get; set; }

        /// <summary>
        /// Gets or sets the number of elements that were unchanged in all branches.
        /// </summary>
        public int UnchangedElements { get; set; }

        /// <summary>
        /// Gets or sets the number of elements merged from the "ours" branch only.
        /// </summary>
        public int OursOnlyChanges { get; set; }

        /// <summary>
        /// Gets or sets the number of elements merged from the "theirs" branch only.
        /// </summary>
        public int TheirsOnlyChanges { get; set; }

        /// <summary>
        /// Gets or sets the number of elements automatically merged from both branches.
        /// </summary>
        public int AutoMergedChanges { get; set; }

        /// <summary>
        /// Gets or sets the number of conflicts that occurred.
        /// </summary>
        public int ConflictCount { get; set; }

        /// <summary>
        /// Gets or sets the number of conflicts resolved automatically.
        /// </summary>
        public int ResolvedConflicts { get; set; }

        /// <summary>
        /// Gets or sets the number of conflicts resolved by the conflict resolver.
        /// </summary>
        public int ResolverResolvedConflicts { get; set; }

        /// <summary>
        /// Gets the total number of changes applied during the merge.
        /// </summary>
        public int TotalChanges => OursOnlyChanges + TheirsOnlyChanges + AutoMergedChanges;

        /// <summary>
        /// Gets the number of unresolved conflicts.
        /// </summary>
        public int UnresolvedConflicts => ConflictCount - ResolvedConflicts - ResolverResolvedConflicts;

        /// <summary>
        /// Creates a string summary of the merge statistics.
        /// </summary>
        /// <returns>A summary string.</returns>
        public override string ToString()
        {
            return $"Merge Statistics: {TotalElements} elements, " +
                   $"{TotalChanges} changes, " +
                   $"{ConflictCount} conflicts " +
                   $"({ResolvedConflicts} auto-resolved, " +
                   $"{ResolverResolvedConflicts} resolver-resolved, " +
                   $"{UnresolvedConflicts} unresolved)";
        }
    }
}
