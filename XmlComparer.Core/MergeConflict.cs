using System;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Represents a conflict that occurred during a three-way merge.
    /// </summary>
    /// <remarks>
    /// <para>A conflict occurs when two branches make incompatible changes to the same element.
    /// The merge engine detects these conflicts and collects them for resolution.</para>
    /// <para>Each conflict contains:</para>
    /// <list type="bullet">
    ///   <item><description>The path to the conflicted element</description></item>
    ///   <item><description>The base (ancestor) element</description></item>
    ///   <item><description>The "ours" branch element</description></item>
    ///   <item><description>The "theirs" branch element</description></item>
    ///   <item><description>The conflict type</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var conflict in mergeResult.Conflicts)
    /// {
    ///     Console.WriteLine($"Conflict at {conflict.Path}:");
    ///     Console.WriteLine($"  Type: {conflict.ConflictType}");
    ///     Console.WriteLine($"  Base: {conflict.BaseElement}");
    ///     Console.WriteLine($"  Ours: {conflict.OursElement}");
    ///     Console.WriteLine($"  Theirs: {conflict.TheirsElement}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="MergeResult"/>
    /// <seealso cref="IMergeConflictResolver"/>
    public class MergeConflict
    {
        /// <summary>
        /// Gets or sets the XPath path to the conflicted element.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base (ancestor) element before either branch made changes.
        /// </summary>
        /// <remarks>
        /// This may be null if the element was added in one or both branches.
        /// </remarks>
        public XElement? BaseElement { get; set; }

        /// <summary>
        /// Gets or sets the element from the "ours" branch (first branch).
        /// </summary>
        /// <remarks>
        /// "Ours" typically refers to the current/local branch. This may be null
        /// if the element was deleted in this branch.
        /// </remarks>
        public XElement? OursElement { get; set; }

        /// <summary>
        /// Gets or sets the element from the "theirs" branch (second branch).
        /// </summary>
        /// <remarks>
        /// "Theirs" typically refers to the incoming/remote branch. This may be null
        /// if the element was deleted in this branch.
        /// </remarks>
        public XElement? TheirsElement { get; set; }

        /// <summary>
        /// Gets or sets the type of conflict that occurred.
        /// </summary>
        public MergeConflictType ConflictType { get; set; }

        /// <summary>
        /// Gets or sets a custom description of the conflict.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets whether both branches added different elements at the same location.
        /// </summary>
        public bool IsAddAddConflict =>
            BaseElement == null && OursElement != null && TheirsElement != null;

        /// <summary>
        /// Gets whether one branch modified while the other deleted the element.
        /// </summary>
        public bool IsModifyDeleteConflict =>
            BaseElement != null &&
            ((OursElement != null && TheirsElement == null) ||
             (OursElement == null && TheirsElement != null));

        /// <summary>
        /// Gets whether both branches modified the same element differently.
        /// </summary>
        public bool IsModifyModifyConflict =>
            BaseElement != null && OursElement != null && TheirsElement != null;

        /// <summary>
        /// Gets a human-readable description of this conflict.
        /// </summary>
        /// <returns>A description string.</returns>
        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Description))
                return Description;

            return ConflictType switch
            {
                MergeConflictType.AddAdd => "Both branches added different elements",
                MergeConflictType.ModifyModify => "Both branches modified the same element",
                MergeConflictType.ModifyDelete => "One branch modified, the other deleted",
                MergeConflictType.DeleteDelete => "Both branches deleted (should not occur)",
                MergeConflictType.AttributeConflict => "Conflicting attribute changes",
                MergeConflictType.NamespaceConflict => "Conflicting namespace changes",
                _ => "Unknown conflict type"
            };
        }

        /// <summary>
        /// Creates a string representation of this conflict.
        /// </summary>
        /// <returns>A string showing the conflict details.</returns>
        public override string ToString()
        {
            return $"Conflict at {Path}: {GetDescription()}";
        }
    }

    /// <summary>
    /// Defines the types of merge conflicts that can occur.
    /// </summary>
    public enum MergeConflictType
    {
        /// <summary>
        /// Both branches added different elements at the same location.
        /// </summary>
        AddAdd,

        /// <summary>
        /// Both branches modified the same element in different ways.
        /// </summary>
        ModifyModify,

        /// <summary>
        /// One branch modified the element while the other deleted it.
        /// </summary>
        ModifyDelete,

        /// <summary>
        /// Both branches deleted the same element.
        /// </summary>
        /// <remarks>
        /// This should not typically occur as it's not a conflict.
        /// </remarks>
        DeleteDelete,

        /// <summary>
        /// The branches have conflicting changes to the same attribute.
        /// </summary>
        AttributeConflict,

        /// <summary>
        /// The branches have conflicting namespace changes.
        /// </summary>
        NamespaceConflict
    }
}
