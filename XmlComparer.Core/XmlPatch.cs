using System;
using System.Collections.Generic;
using System.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Represents a collection of patch operations that can be applied to an XML document.
    /// </summary>
    /// <remarks>
    /// <para>An XmlPatch is a serializable representation of changes between two XML documents.
    /// It contains a list of operations that transform the original document into the new document.</para>
    /// <para>Patches can be:</para>
    /// <list type="bullet">
    ///   <item><description>Generated from diff results</description></item>
    ///   <item>Serialized to XML or JSON for storage/transmission</item>
    ///   <item>Deserialized and applied to documents</item>
    ///   <item>Combined with other patches</item>
    ///   <item>Reversed to create undo patches</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Generate a patch from a diff
    /// var generator = new XmlPatchGenerator();
    /// var patch = generator.Generate(diffResult);
    ///
    /// // Serialize to XML
    /// string xml = patch.ToXml();
    /// File.WriteAllText("changes.patch", xml);
    ///
    /// // Deserialize and apply
    /// var loadedPatch = XmlPatch.FromXml(xml);
    /// var applier = new XmlPatchApplier();
    /// var result = applier.Apply(patch, originalDocument);
    /// </code>
    /// </example>
    /// <seealso cref="XmlPatchOperation"/>
    /// <seealso cref="XmlPatchGenerator"/>
    /// <seealso cref="XmlPatchApplier"/>
    public class XmlPatch
    {
        /// <summary>
        /// Gets or sets the unique identifier for this patch.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the human-readable title for this patch.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description of this patch.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the original file path/name that this patch was generated from.
        /// </summary>
        public string? OriginalFile { get; set; }

        /// <summary>
        /// Gets or sets the target file path/name for this patch.
        /// </summary>
        public string? TargetFile { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this patch was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the author of this patch.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the list of patch operations.
        /// </summary>
        public List<XmlPatchOperation> Operations { get; set; } = new List<XmlPatchOperation>();

        /// <summary>
        /// Gets or sets the metadata for this patch.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the number of operations in this patch.
        /// </summary>
        public int OperationCount => Operations.Count;

        /// <summary>
        /// Gets statistics about this patch.
        /// </summary>
        public PatchStatistics Statistics => CalculateStatistics();

        /// <summary>
        /// Creates a new empty XmlPatch.
        /// </summary>
        public XmlPatch() { }

        /// <summary>
        /// Creates a new XmlPatch with the specified operations.
        /// </summary>
        /// <param name="operations">The operations to include.</param>
        public XmlPatch(IEnumerable<XmlPatchOperation> operations)
        {
            Operations = new List<XmlPatchOperation>(operations);
        }

        /// <summary>
        /// Adds an operation to this patch.
        /// </summary>
        /// <param name="operation">The operation to add.</param>
        public void AddOperation(XmlPatchOperation operation)
        {
            Operations.Add(operation);
        }

        /// <summary>
        /// Adds multiple operations to this patch.
        /// </summary>
        /// <param name="operations">The operations to add.</param>
        public void AddOperations(IEnumerable<XmlPatchOperation> operations)
        {
            Operations.AddRange(operations);
        }

        /// <summary>
        /// Removes an operation from this patch.
        /// </summary>
        /// <param name="operation">The operation to remove.</param>
        /// <returns>True if the operation was found and removed.</returns>
        public bool RemoveOperation(XmlPatchOperation operation)
        {
            return Operations.Remove(operation);
        }

        /// <summary>
        /// Creates a reverse patch that undoes the changes in this patch.
        /// </summary>
        /// <returns>A new patch that reverses this patch's operations.</returns>
        public XmlPatch Reverse()
        {
            var reverse = new XmlPatch
            {
                Id = Id + "-reverse",
                Title = "Reverse of: " + (Title ?? ""),
                Description = "Reverses: " + (Description ?? ""),
                OriginalFile = TargetFile,
                TargetFile = OriginalFile,
                CreatedAt = DateTime.UtcNow,
                Author = Author,
                Metadata = new Dictionary<string, string>(Metadata)
            };

            foreach (var op in Operations)
            {
                reverse.Operations.Add(ReverseOperation(op));
            }

            // Reverse the order of operations
            reverse.Operations.Reverse();
            return reverse;
        }

        /// <summary>
        /// Reverses a single operation.
        /// </summary>
        private XmlPatchOperation ReverseOperation(XmlPatchOperation op)
        {
            return op.Type switch
            {
                PatchOperationType.Add => XmlPatchOperation.Remove(op.TargetPath),
                PatchOperationType.Remove => XmlPatchOperation.Add(op.TargetPath, op.Content ?? "", PatchPosition.Replace),
                PatchOperationType.Replace => XmlPatchOperation.Replace(op.TargetPath, op.OldValue ?? "", op.NewValue),
                PatchOperationType.Move => XmlPatchOperation.Move(op.NewValue ?? "", op.TargetPath),
                _ => new XmlPatchOperation { Type = op.Type, TargetPath = op.TargetPath }
            };
        }

        /// <summary>
        /// Combines this patch with another patch.
        /// </summary>
        /// <param name="other">The patch to combine with.</param>
        /// <returns>A new patch containing all operations from both patches.</returns>
        public XmlPatch Combine(XmlPatch other)
        {
            var combined = new XmlPatch
            {
                Id = Id + "+" + other.Id,
                Title = "Combined patch",
                Operations = new List<XmlPatchOperation>(Operations)
            };

            combined.Operations.AddRange(other.Operations);
            return combined;
        }

        /// <summary>
        /// Serializes this patch to XML format.
        /// </summary>
        /// <returns>An XML string representation of this patch.</returns>
        public string ToXml()
        {
            return XmlPatchFormatter.SerializeToXml(this);
        }

        /// <summary>
        /// Serializes this patch to JSON format.
        /// </summary>
        /// <returns>A JSON string representation of this patch.</returns>
        public string ToJson()
        {
            return XmlPatchFormatter.SerializeToJson(this);
        }

        /// <summary>
        /// Deserializes a patch from XML format.
        /// </summary>
        /// <param name="xml">The XML string to deserialize.</param>
        /// <returns>A deserialized XmlPatch.</returns>
        public static XmlPatch FromXml(string xml)
        {
            return XmlPatchFormatter.DeserializeFromXml(xml);
        }

        /// <summary>
        /// Deserializes a patch from JSON format.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A deserialized XmlPatch.</returns>
        public static XmlPatch FromJson(string json)
        {
            return XmlPatchFormatter.DeserializeFromJson(json);
        }

        /// <summary>
        /// Creates a string representation of this patch.
        /// </summary>
        /// <returns>A summary string.</returns>
        public override string ToString()
        {
            return $"Patch '{Title ?? Id}': {OperationCount} operations, {Statistics.TotalChanges} total changes";
        }

        /// <summary>
        /// Calculates statistics for this patch.
        /// </summary>
        private PatchStatistics CalculateStatistics()
        {
            var stats = new PatchStatistics();

            foreach (var op in Operations)
            {
                switch (op.Type)
                {
                    case PatchOperationType.Add:
                        stats.Additions++;
                        break;
                    case PatchOperationType.Remove:
                        stats.Deletions++;
                        break;
                    case PatchOperationType.Replace:
                        stats.Replacements++;
                        break;
                    case PatchOperationType.Move:
                        stats.Moves++;
                        break;
                }
                stats.TotalChanges++;
            }

            return stats;
        }
    }

    /// <summary>
    /// Statistics for an XML patch.
    /// </summary>
    public class PatchStatistics
    {
        /// <summary>
        /// Gets or sets the number of add operations.
        /// </summary>
        public int Additions { get; set; }

        /// <summary>
        /// Gets or sets the number of remove operations.
        /// </summary>
        public int Deletions { get; set; }

        /// <summary>
        /// Gets or sets the number of replace operations.
        /// </summary>
        public int Replacements { get; set; }

        /// <summary>
        /// Gets or sets the number of move operations.
        /// </summary>
        public int Moves { get; set; }

        /// <summary>
        /// Gets or sets the total number of operations.
        /// </summary>
        public int TotalChanges { get; set; }

        /// <summary>
        /// Creates a string summary of the statistics.
        /// </summary>
        public override string ToString()
        {
            return $"{TotalChanges} operations ({Additions} additions, {Deletions} deletions, {Replacements} replacements, {Moves} moves)";
        }
    }
}
