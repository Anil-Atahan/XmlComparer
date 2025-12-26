using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates XML patches from diff results.
    /// </summary>
    /// <remarks>
    /// <para>The XmlPatchGenerator converts <see cref="DiffMatch"/> trees into serializable
    /// <see cref="XmlPatch"/> objects that can be stored, transmitted, and applied later.</para>
    /// <para>Generated patches include:</para>
    /// <list type="bullet">
    ///   <item><description>Add operations for new elements</description></item>
    ///   <item><description>Remove operations for deleted elements</description></item>
    ///   <item><description>Replace operations for modified elements</description></item>
    ///   <item><description>Move operations for moved elements</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var comparer = new XmlComparerService();
    /// var diff = comparer.Compare("original.xml", "modified.xml");
    ///
    /// var generator = new XmlPatchGenerator();
    /// var patch = generator.Generate(diff, "original.xml", "modified.xml");
    ///
    /// // Save to file
    /// File.WriteAllText("changes.patch", patch.ToXml());
    /// </code>
    /// </example>
    /// <seealso cref="XmlPatch"/>
    /// <seealso cref="XmlPatchApplier"/>
    public class XmlPatchGenerator
    {
        /// <summary>
        /// Gets or sets the options for patch generation.
        /// </summary>
        public PatchGeneratorOptions Options { get; set; } = new PatchGeneratorOptions();

        /// <summary>
        /// Creates a new XmlPatchGenerator with default options.
        /// </summary>
        public XmlPatchGenerator() { }

        /// <summary>
        /// Creates a new XmlPatchGenerator with the specified options.
        /// </summary>
        /// <param name="options">The generator options.</param>
        public XmlPatchGenerator(PatchGeneratorOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Generates a patch from a diff result.
        /// </summary>
        /// <param name="diff">The diff result to convert.</param>
        /// <param name="originalFile">The original file path (for metadata).</param>
        /// <param name="targetFile">The target file path (for metadata).</param>
        /// <returns>A generated XmlPatch.</returns>
        public XmlPatch Generate(DiffMatch diff, string? originalFile = null, string? targetFile = null)
        {
            var patch = new XmlPatch
            {
                Id = Guid.NewGuid().ToString(),
                Title = Options.DefaultTitle ?? "XML Patch",
                Description = Options.DefaultDescription,
                OriginalFile = originalFile,
                TargetFile = targetFile,
                CreatedAt = DateTime.UtcNow,
                Author = Options.Author
            };

            GenerateOperations(diff, patch);
            return patch;
        }

        /// <summary>
        /// Generates patch operations by traversing the diff tree.
        /// </summary>
        private void GenerateOperations(DiffMatch node, XmlPatch patch)
        {
            if (node.Type == DiffType.Unchanged && !Options.IncludeUnchanged)
            {
                // Recursively process children even if parent is unchanged
                foreach (var child in node.Children)
                {
                    GenerateOperations(child, patch);
                }
                return;
            }

            switch (node.Type)
            {
                case DiffType.Added:
                    GenerateAddOperation(node, patch);
                    break;

                case DiffType.Deleted:
                    GenerateDeleteOperation(node, patch);
                    break;

                case DiffType.Modified:
                    if (Options.GenerateReplaceForModifies)
                    {
                        GenerateReplaceOperation(node, patch);
                    }
                    else
                    {
                        // Generate delete + add for modifications
                        GenerateDeleteOperation(node, patch);
                        GenerateAddOperation(node, patch);
                    }
                    break;

                case DiffType.Moved:
                    if (Options.GenerateMoveOperations)
                    {
                        GenerateMoveOperation(node, patch);
                    }
                    else
                    {
                        // Treat moved as delete + add
                        GenerateDeleteOperation(node, patch);
                        GenerateAddOperation(node, patch);
                    }
                    break;

                case DiffType.NamespaceChanged:
                    GenerateNamespaceChangeOperation(node, patch);
                    break;
            }

            // Always process children
            foreach (var child in node.Children)
            {
                GenerateOperations(child, patch);
            }
        }

        /// <summary>
        /// Generates an Add operation for an added element.
        /// </summary>
        private void GenerateAddOperation(DiffMatch node, XmlPatch patch)
        {
            if (node.NewElement == null) return;

            string parentPath = GetParentPath(node.Path ?? "");
            string content = node.NewElement.ToString(SaveOptions.DisableFormatting);

            var operation = XmlPatchOperation.Add(parentPath, content, PatchPosition.End);

            patch.AddOperation(operation);
        }

        /// <summary>
        /// Generates a Delete operation for a deleted element.
        /// </summary>
        private void GenerateDeleteOperation(DiffMatch node, XmlPatch patch)
        {
            string targetPath = node.Path ?? "";

            var operation = XmlPatchOperation.Remove(targetPath);

            // Store old value for verification
            if (node.OriginalElement != null)
            {
                operation.OldValue = node.OriginalElement.ToString(SaveOptions.DisableFormatting);
            }

            patch.AddOperation(operation);
        }

        /// <summary>
        /// Generates a Replace operation for a modified element.
        /// </summary>
        private void GenerateReplaceOperation(DiffMatch node, XmlPatch patch)
        {
            if (node.NewElement == null) return;

            string targetPath = node.Path ?? "";
            string newValue = node.NewElement.ToString(SaveOptions.DisableFormatting);
            string? oldValue = null;

            if (node.OriginalElement != null)
            {
                oldValue = node.OriginalElement.ToString(SaveOptions.DisableFormatting);
            }

            var operation = XmlPatchOperation.Replace(targetPath, newValue, oldValue);

            patch.AddOperation(operation);
        }

        /// <summary>
        /// Generates a Move operation for a moved element.
        /// </summary>
        private void GenerateMoveOperation(DiffMatch node, XmlPatch patch)
        {
            // We need to know both the old and new positions
            // For now, we'll use the path information
            string currentPath = node.Path ?? "";

            var operation = new XmlPatchOperation
            {
                Type = PatchOperationType.Move,
                TargetPath = currentPath
            };

            patch.AddOperation(operation);
        }

        /// <summary>
        /// Generates a namespace change operation.
        /// </summary>
        private void GenerateNamespaceChangeOperation(DiffMatch node, XmlPatch patch)
        {
            // Namespace changes are treated as replace operations
            GenerateReplaceOperation(node, patch);
        }

        /// <summary>
        /// Gets the parent path from a full element path.
        /// </summary>
        private string GetParentPath(string path)
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash > 0)
            {
                return path.Substring(0, lastSlash);
            }
            return "/";
        }

        /// <summary>
        /// Generates a minimal patch with only the essential operations.
        /// </summary>
        /// <param name="diff">The diff result.</param>
        /// <param name="originalFile">The original file path.</param>
        /// <param name="targetFile">The target file path.</param>
        /// <returns>A minimal patch.</returns>
        public XmlPatch GenerateMinimal(DiffMatch diff, string? originalFile = null, string? targetFile = null)
        {
            var originalOptions = Options;
            Options = new PatchGeneratorOptions
            {
                IncludeUnchanged = false,
                GenerateMoveOperations = true,
                GenerateReplaceForModifies = true,
                Author = originalOptions.Author,
                DefaultTitle = originalOptions.DefaultTitle,
                DefaultDescription = originalOptions.DefaultDescription
            };

            var patch = Generate(diff, originalFile, targetFile);
            Options = originalOptions;
            return patch;
        }

        /// <summary>
        /// Generates a verbose patch with detailed information.
        /// </summary>
        /// <param name="diff">The diff result.</param>
        /// <param name="originalFile">The original file path.</param>
        /// <param name="targetFile">The target file path.</param>
        /// <returns>A verbose patch.</returns>
        public XmlPatch GenerateVerbose(DiffMatch diff, string? originalFile = null, string? targetFile = null)
        {
            var originalOptions = Options;
            Options = new PatchGeneratorOptions
            {
                IncludeUnchanged = false,
                GenerateMoveOperations = true,
                GenerateReplaceForModifies = false,
                Author = originalOptions.Author,
                DefaultTitle = originalOptions.DefaultTitle,
                DefaultDescription = originalOptions.DefaultDescription
            };

            var patch = Generate(diff, originalFile, targetFile);
            Options = originalOptions;
            return patch;
        }
    }

    /// <summary>
    /// Options for patch generation.
    /// </summary>
    public class PatchGeneratorOptions
    {
        /// <summary>
        /// Gets or sets whether to include unchanged elements in the patch.
        /// </summary>
        /// <remarks>
        /// When true, all elements are included. When false, only changes are included.
        /// Default is false.
        /// </remarks>
        public bool IncludeUnchanged { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to generate Move operations.
        /// </summary>
        /// <remarks>
        /// When false, moves are represented as delete + add operations.
        /// Default is true.
        /// </remarks>
        public bool GenerateMoveOperations { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate Replace operations for modifications.
        /// </summary>
        /// <remarks>
        /// When false, modifications are represented as delete + add operations.
        /// Default is true.
        /// </remarks>
        public bool GenerateReplaceForModifies { get; set; } = true;

        /// <summary>
        /// Gets or sets the default title for generated patches.
        /// </summary>
        public string? DefaultTitle { get; set; }

        /// <summary>
        /// Gets or sets the default description for generated patches.
        /// </summary>
        public string? DefaultDescription { get; set; }

        /// <summary>
        /// Gets or sets the author for generated patches.
        /// </summary>
        public string? Author { get; set; }
    }
}
