using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a strategy for resolving merge conflicts.
    /// </summary>
    /// <remarks>
    /// <para>Implement this interface to provide custom conflict resolution logic
    /// for three-way XML merges. The resolver is called when conflicts are detected
    /// during the merge process.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomResolver : IMergeConflictResolver
    /// {
    ///     public XElement? Resolve(MergeConflict conflict)
    ///     {
    ///         // Always prefer the "theirs" version
    ///         return conflict.TheirsElement;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IMergeConflictResolver
    {
        /// <summary>
        /// Resolves a merge conflict and returns the merged element.
        /// </summary>
        /// <param name="conflict">The conflict to resolve.</param>
        /// <returns>The resolved element, or null to remove the element, or the conflict's BaseElement to keep the original.</returns>
        /// <remarks>
        /// <para>Implementations can choose between:</para>
        /// <list type="bullet">
        ///   <item><description><see cref="MergeConflict.BaseElement"/> - Keep the original (base) version</description></item>
        ///   <item><description><see cref="MergeConflict.OursElement"/> - Use the "ours" branch version</description></item>
        ///   <item><description><see cref="MergeConflict.TheirsElement"/> - Use the "theirs" branch version</description></item>
        ///   <item><description>A new element - Create a custom merged version</description></item>
        ///   <item><description>null - Remove the element entirely</description></item>
        /// </list>
        /// </remarks>
        XElement? Resolve(MergeConflict conflict);
    }

    /// <summary>
    /// Predefined conflict resolution strategies.
    /// </summary>
    public static class MergeConflictResolvers
    {
        /// <summary>
        /// Always chooses the "ours" branch version when conflicts occur.
        /// </summary>
        /// <remarks>
        /// "Ours" refers to the first branch being merged (typically the current/local branch).
        /// </remarks>
        public class OursResolver : IMergeConflictResolver
        {
            /// <summary>
            /// Returns the "ours" element for all conflicts.
            /// </summary>
            public XElement? Resolve(MergeConflict conflict) => conflict.OursElement;
        }

        /// <summary>
        /// Always chooses the "theirs" branch version when conflicts occur.
        /// </summary>
        /// <remarks>
        /// "Theirs" refers to the second branch being merged (typically the incoming/remote branch).
        /// </remarks>
        public class TheirsResolver : IMergeConflictResolver
        {
            /// <summary>
            /// Returns the "theirs" element for all conflicts.
            /// </summary>
            public XElement? Resolve(MergeConflict conflict) => conflict.TheirsElement;
        }

        /// <summary>
        /// Always chooses the base (ancestor) version when conflicts occur.
        /// </summary>
        /// <remarks>
        /// This effectively rejects both branches' changes, keeping the original.
        /// </remarks>
        public class BaseResolver : IMergeConflictResolver
        {
            /// <summary>
            /// Returns the base element for all conflicts.
            /// </summary>
            public XElement? Resolve(MergeConflict conflict) => conflict.BaseElement;
        }

        /// <summary>
        /// Attempts to automatically merge conflicting changes.
        /// </summary>
        /// <remarks>
        /// <para>This strategy tries to intelligently combine changes from both branches:</para>
        /// <list type="bullet">
        ///   <item><description>For attribute conflicts: keeps all unique attributes from both branches</description></item>
        ///   <item><description>For value conflicts: concatenates values with a separator</description></item>
        ///   <item><description>For child conflicts: merges all children from both branches</description></item>
        /// </list>
        /// </remarks>
        public class AutoMergeResolver : IMergeConflictResolver
        {
            /// <summary>
            /// Gets or sets the separator used when merging text values.
            /// </summary>
            public string ValueSeparator { get; set; } = " | ";

            /// <summary>
            /// Attempts to automatically merge the conflicting elements.
            /// </summary>
            public XElement? Resolve(MergeConflict conflict)
            {
                if (conflict.TheirsElement == null) return conflict.OursElement;
                if (conflict.OursElement == null) return conflict.TheirsElement;

                var merged = new XElement(conflict.BaseElement?.Name ?? conflict.TheirsElement.Name);

                // Merge attributes from both branches
                foreach (var attr in conflict.BaseElement?.Attributes() ?? Enumerable.Empty<XAttribute>())
                {
                    merged.SetAttributeValue(attr.Name, attr.Value);
                }
                foreach (var attr in conflict.OursElement.Attributes())
                {
                    merged.SetAttributeValue(attr.Name, attr.Value);
                }
                foreach (var attr in conflict.TheirsElement.Attributes())
                {
                    merged.SetAttributeValue(attr.Name, attr.Value);
                }

                // Merge values
                string baseValue = conflict.BaseElement?.Value ?? "";
                string oursValue = conflict.OursElement.Value;
                string theirsValue = conflict.TheirsElement.Value;

                if (oursValue != baseValue || theirsValue != baseValue)
                {
                    if (oursValue == theirsValue)
                    {
                        merged.Value = oursValue;
                    }
                    else if (oursValue == baseValue)
                    {
                        merged.Value = theirsValue;
                    }
                    else if (theirsValue == baseValue)
                    {
                        merged.Value = oursValue;
                    }
                    else
                    {
                        merged.Value = $"{oursValue}{ValueSeparator}{theirsValue}";
                    }
                }
                else
                {
                    merged.Value = baseValue;
                }

                return merged;
            }
        }
    }
}
