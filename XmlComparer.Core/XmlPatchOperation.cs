using System;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines the types of patch operations.
    /// </summary>
    public enum PatchOperationType
    {
        /// <summary>
        /// Adds a new element or attribute.
        /// </summary>
        Add,

        /// <summary>
        /// Removes an existing element or attribute.
        /// </summary>
        Remove,

        /// <summary>
        /// Replaces an element or attribute with a new value.
        /// </summary>
        Replace,

        /// <summary>
        /// Moves an element to a new location.
        /// </summary>
        Move,

        /// <summary>
        /// Changes the namespace of an element.
        /// </summary>
        ChangeNamespace
    }

    /// <summary>
    /// Represents a single operation in an XML patch.
    /// </summary>
    /// <remarks>
    /// <para>Patch operations are atomic changes that can be applied to an XML document.
    /// Each operation specifies:</para>
    /// <list type="bullet">
    ///   <item><description>The type of operation (add, remove, replace, move)</description></item>
    ///   <item><description>The target path (XPath) where the operation should be applied</description></item>
    ///   <item><description>The content to add, remove, or replace</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Add a new element
    /// var addOp = new XmlPatchOperation
    /// {
    ///     Type = PatchOperationType.Add,
    ///     TargetPath = "/root/users",
    ///     Content = "&lt;user id=\"new\"/&gt;",
    ///     Position = PatchPosition.End
    /// };
    ///
    /// // Remove an element
    /// var removeOp = new XmlPatchOperation
    /// {
    ///     Type = PatchOperationType.Remove,
    ///     TargetPath = "/root/users/user[@id='old']"
    /// };
    ///
    /// // Replace an element's value
    /// var replaceOp = new XmlPatchOperation
    /// {
    ///     Type = PatchOperationType.Replace,
    ///     TargetPath = "/root/config/setting[@name='timeout']",
    ///     NewValue = "30"
    /// };
    /// </code>
    /// </example>
    public class XmlPatchOperation
    {
        /// <summary>
        /// Gets or sets the type of operation.
        /// </summary>
        public PatchOperationType Type { get; set; }

        /// <summary>
        /// Gets or sets the XPath path to the target element or attribute.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the XML content to add or replace.
        /// </summary>
        /// <remarks>
        /// This is a string containing XML markup. For add operations, this is the element
        /// or attribute to add. For replace operations, this is the new content.
        /// </remarks>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets the new value for replace operations.
        /// </summary>
        /// <remarks>
        /// This is used for simple value replacements (e.g., text content or attribute values).
        /// </remarks>
        public string? NewValue { get; set; }

        /// <summary>
        /// Gets or sets the position where to add the content (for Add operations).
        /// </summary>
        public PatchPosition Position { get; set; } = PatchPosition.End;

        /// <summary>
        /// Gets or sets the old value for verification (for Replace operations).
        /// </summary>
        /// <remarks>
        /// When set, the patch will only apply if the current value matches this.
        /// This prevents accidental overwrites of unexpected values.
        /// </remarks>
        public string? OldValue { get; set; }

        /// <summary>
        /// Gets or sets a condition that must be true for the operation to apply.
        /// </summary>
        /// <remarks>
        /// This is an XPath expression that must evaluate to true for the operation
        /// to be applied. Useful for conditional patching.
        /// </remarks>
        public string? Condition { get; set; }

        /// <summary>
        /// Gets or sets the line number where this operation originated (for debugging).
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// Gets or sets a description of this operation.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Creates a new Add operation.
        /// </summary>
        /// <param name="targetPath">The parent element's path.</param>
        /// <param name="content">The XML content to add.</param>
        /// <param name="position">Where to insert the content.</param>
        /// <returns>A new Add operation.</returns>
        public static XmlPatchOperation Add(string targetPath, string content, PatchPosition position = PatchPosition.End)
        {
            return new XmlPatchOperation
            {
                Type = PatchOperationType.Add,
                TargetPath = targetPath,
                Content = content,
                Position = position
            };
        }

        /// <summary>
        /// Creates a new Remove operation.
        /// </summary>
        /// <param name="targetPath">The path to the element to remove.</param>
        /// <returns>A new Remove operation.</returns>
        public static XmlPatchOperation Remove(string targetPath)
        {
            return new XmlPatchOperation
            {
                Type = PatchOperationType.Remove,
                TargetPath = targetPath
            };
        }

        /// <summary>
        /// Creates a new Replace operation.
        /// </summary>
        /// <param name="targetPath">The path to the element to replace.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="oldValue">Optional old value for verification.</param>
        /// <returns>A new Replace operation.</returns>
        public static XmlPatchOperation Replace(string targetPath, string newValue, string? oldValue = null)
        {
            return new XmlPatchOperation
            {
                Type = PatchOperationType.Replace,
                TargetPath = targetPath,
                NewValue = newValue,
                OldValue = oldValue
            };
        }

        /// <summary>
        /// Creates a new Move operation.
        /// </summary>
        /// <param name="targetPath">The path to the element to move.</param>
        /// <param name="newPath">The new path where to move the element.</param>
        /// <returns>A new Move operation.</returns>
        public static XmlPatchOperation Move(string targetPath, string newPath)
        {
            return new XmlPatchOperation
            {
                Type = PatchOperationType.Move,
                TargetPath = targetPath,
                NewValue = newPath // NewValue reused as destination path
            };
        }

        /// <summary>
        /// Returns a string representation of this operation.
        /// </summary>
        public override string ToString()
        {
            return $"{Type} {TargetPath}";
        }
    }

    /// <summary>
    /// Defines where to insert new content relative to the target.
    /// </summary>
    public enum PatchPosition
    {
        /// <summary>
        /// Insert at the beginning of the target's children.
        /// </summary>
        Start,

        /// <summary>
        /// Insert at the end of the target's children (default).
        /// </summary>
        End,

        /// <summary>
        /// Insert before the target element.
        /// </summary>
        Before,

        /// <summary>
        /// Insert after the target element.
        /// </summary>
        After,

        /// <summary>
        /// Replace the target element entirely.
        /// </summary>
        Replace
    }
}
