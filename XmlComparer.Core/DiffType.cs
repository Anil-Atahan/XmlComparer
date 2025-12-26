namespace XmlComparer.Core
{
    /// <summary>
    /// Defines the types of differences that can be detected between XML elements.
    /// </summary>
    /// <remarks>
    /// <para>The comparison engine assigns one of these types to each <see cref="DiffMatch"/> node
    /// to indicate how the element differs between the original and new documents.</para>
    /// <para>These types are used in the diff tree and in HTML/JSON reports for visual representation.</para>
    /// </remarks>
    /// <seealso cref="DiffMatch"/>
    /// <seealso cref="DiffSummary"/>
    public enum DiffType
    {
        /// <summary>
        /// No differences detected between the elements.
        /// </summary>
        Unchanged,

        /// <summary>
        /// The element exists only in the new document (added).
        /// </summary>
        Added,

        /// <summary>
        /// The element exists only in the original document (deleted).
        /// </summary>
        Deleted,

        /// <summary>
        /// The element exists in both documents but has different attributes, text content, or child structure (modified).
        /// </summary>
        Modified,

        /// <summary>
        /// The element exists in both documents at different positions (moved).
        /// Move detection requires key attributes to be configured.
        /// </summary>
        /// <example>
        /// <code>
        /// // Enable move detection with key attributes
        /// options.WithKeyAttributes("id");
        /// </code>
        /// </example>
        Moved,

        /// <summary>
        /// The element's namespace has changed between documents.
        /// Namespace changes are detected when <see cref="XmlComparisonOptions.WithNamespaceComparison(NamespaceComparisonMode)"/>
        /// is set to <see cref="NamespaceComparisonMode.UriSensitive"/> or <see cref="NamespaceComparisonMode.PrefixPreserve"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// // Enable namespace change detection
        /// options.WithNamespaceComparison(NamespaceComparisonMode.UriSensitive);
        /// </code>
        /// </example>
        /// <seealso cref="NamespaceComparisonMode"/>
        NamespaceChanged,

        /// <summary>
        /// The element has conflicting changes in a three-way merge.
        /// This indicates that both branches modified the same element in incompatible ways.
        /// </summary>
        /// <remarks>
        /// <para>Conflicts occur during three-way merge when:</para>
        /// <list type="bullet">
        ///   <item><description>Both branches modified the same element differently</description></item>
        ///   <item><description>One branch modified while the other deleted the same element</description></item>
        ///   <item><description>Both branches added different elements with the same key at the same location</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Three-way merge with conflicts
        /// var result = XmlComparer.Merge(baseXml, branch1Xml, branch2Xml);
        /// if (result.HasConflicts)
        /// {
        ///     foreach (var conflict in result.Conflicts)
        ///     {
        ///         Console.WriteLine($"Conflict at {conflict.Path}");
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="MergeConflict"/>
        Conflict
    }
}
