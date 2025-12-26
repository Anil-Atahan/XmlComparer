using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates unified diff format output from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>Unified diff format is a standard format used by version control systems
    /// like Git, SVN, and Mercurial. It presents changes in a way that can be
    /// applied using the patch command.</para>
    /// <para>The XML-specific unified diff format:</para>
    /// <list type="bullet">
    ///   <item><description>Uses XML tags as the "lines" to compare</description></item>
    ///   <item><description>Shows context elements around changes</description></item>
    ///   <item><description>Indicates additions (+) and deletions (-)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new UnifiedDiffFormatter();
    /// string diff = formatter.Format(diffResult);
    ///
    /// // With file names in header
    /// string diff = formatter.FormatWithHeader(diffResult, "original.xml", "new.xml");
    /// </code>
    /// </example>
    public class UnifiedDiffFormatter : IDiffFormatter
    {
        private const int DefaultContextLines = 3;

        /// <summary>
        /// Gets or sets the number of context lines to include around changes.
        /// </summary>
        /// <remarks>
        /// Context lines are unchanged elements that appear before and after changes
        /// to provide context for the diff. The default is 3.
        /// </remarks>
        public int ContextLines { get; set; } = DefaultContextLines;

        /// <summary>
        /// Gets or sets whether to include the file header with paths.
        /// </summary>
        public bool IncludeHeader { get; set; } = true;

        /// <summary>
        /// Formats a diff tree into a unified diff string.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>A unified diff formatted string.</returns>
        /// <example>
        /// <code>
        /// var formatter = new UnifiedDiffFormatter();
        /// string diff = formatter.Format(diffResult);
        /// </code>
        /// </example>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into a unified diff string with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>A unified diff formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            var sb = new StringBuilder();
            var hunks = BuildHunks(diff);
            string originalPath = context?.EmbeddedJson?.Contains("original") == true ? "original.xml" : "a.xml";
            string newPath = context?.EmbeddedJson?.Contains("new") == true ? "new.xml" : "b.xml";

            if (IncludeHeader)
            {
                sb.AppendLine($"--- {originalPath}");
                sb.AppendLine($"+++ {newPath}");
            }

            foreach (var hunk in hunks)
            {
                foreach (var line in hunk.GetFormattedLines())
                {
                    sb.AppendLine(line);
                }
                sb.AppendLine(); // Blank line between hunks
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats a diff tree with explicit file path headers.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="originalFile">The path to display for the original file.</param>
        /// <param name="newFile">The path to display for the new file.</param>
        /// <returns>A unified diff formatted string with headers.</returns>
        /// <example>
        /// <code>
        /// var formatter = new UnifiedDiffFormatter();
        /// string diff = formatter.FormatWithHeader(
        ///     diffResult,
        ///     "/path/to/original.xml",
        ///     "/path/to/new.xml"
        /// );
        /// </code>
        /// </example>
        public string FormatWithHeader(DiffMatch diff, string originalFile, string newFile)
        {
            return Format(diff, new FormatterContext
            {
                // Store file info in embedded JSON for the formatter to read
                EmbeddedJson = $"{{\"originalFile\":\"{originalFile}\",\"newFile\":\"{newFile}\"}}"
            });
        }

        /// <summary>
        /// Generates a patch file that can be applied using the patch command.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="originalFile">The path to the original file (for the patch header).</param>
        /// <returns>A patch file in unified diff format.</returns>
        /// <example>
        /// <code>
        /// var formatter = new UnifiedDiffFormatter();
        /// File.WriteAllText("changes.patch", formatter.GeneratePatch(diff, "data.xml"));
        /// </code>
        /// </example>
        public string GeneratePatch(DiffMatch diff, string originalFile)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# XML patch generated by XmlComparer on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");
            sb.AppendLine();
            sb.Append(FormatWithHeader(diff, originalFile, "patched"));
            return sb.ToString();
        }

        /// <summary>
        /// Builds hunks from the diff tree.
        /// </summary>
        private List<DiffHunk> BuildHunks(DiffMatch root)
        {
            var hunks = new List<DiffHunk>();
            var currentHunk = new DiffHunk();
            var contextBuffer = new Queue<DiffMatch>();

            int originalLine = 1;
            int newLine = 1;

            void FlushContext()
            {
                while (contextBuffer.Count > 0)
                {
                    var contextNode = contextBuffer.Dequeue();
                    currentHunk.AddContext(GetNodeContent(contextNode), originalLine++, newLine++);
                }
            }

            void StartNewHunk(DiffMatch startNode)
            {
                if (currentHunk.Lines.Count > 0)
                {
                    currentHunk.OriginalStart = Math.Max(1, currentHunk.OriginalStart);
                    currentHunk.NewStart = Math.Max(1, currentHunk.NewStart);
                    hunks.Add(currentHunk);
                }

                currentHunk = new DiffHunk
                {
                    OriginalStart = originalLine,
                    NewStart = newLine
                };
            }

            ProcessDiffRecursive(root, ref originalLine, ref newLine, contextBuffer, currentHunk, hunks, StartNewHunk, FlushContext);

            // Flush remaining context
            FlushContext();

            // Add the last hunk if it has any changes
            if (currentHunk.Lines.Any(l => l.Type != DiffLineType.Context))
            {
                currentHunk.OriginalStart = Math.Max(1, currentHunk.OriginalStart);
                currentHunk.NewStart = Math.Max(1, currentHunk.NewStart);
                hunks.Add(currentHunk);
            }

            return hunks;
        }

        /// <summary>
        /// Recursively processes the diff tree to build hunks.
        /// </summary>
        private void ProcessDiffRecursive(
            DiffMatch node,
            ref int originalLine,
            ref int newLine,
            Queue<DiffMatch> contextBuffer,
            DiffHunk currentHunk,
            List<DiffHunk> hunks,
            Action<DiffMatch> startNewHunk,
            Action flushContext)
        {
            // Process current node
            if (node.Type != DiffType.Unchanged)
            {
                // Start a new hunk if this is the first change after context
                bool hasOnlyContextSoFar = currentHunk.Lines.All(l => l.Type == DiffLineType.Context);
                if (hasOnlyContextSoFar && contextBuffer.Count > ContextLines)
                {
                    // Trim excess context
                    while (contextBuffer.Count > ContextLines)
                    {
                        contextBuffer.Dequeue();
                    }
                }
                flushContext();

                // Add the changed element
                switch (node.Type)
                {
                    case DiffType.Deleted:
                        currentHunk.AddDeletion(GetNodeContent(node), originalLine++);
                        break;
                    case DiffType.Added:
                        currentHunk.AddAddition(GetNodeContent(node), newLine++);
                        break;
                    case DiffType.Modified:
                        currentHunk.AddDeletion(GetNodeContent(node), originalLine++);
                        currentHunk.AddAddition(GetNodeContent(node), newLine++);
                        break;
                    case DiffType.Moved:
                    case DiffType.NamespaceChanged:
                        // Treat moved/namespace-changed as delete + add
                        currentHunk.AddDeletion(GetNodeContent(node), originalLine++);
                        currentHunk.AddAddition(GetNodeContent(node), newLine++);
                        break;
                }
            }
            else
            {
                contextBuffer.Enqueue(node);
            }

            // Process children recursively
            foreach (var child in node.Children)
            {
                ProcessDiffRecursive(child, ref originalLine, ref newLine, contextBuffer, currentHunk, hunks, startNewHunk, flushContext);
            }
        }

        /// <summary>
        /// Gets a string representation of a diff node.
        /// </summary>
        private static string GetNodeContent(DiffMatch node)
        {
            var sb = new StringBuilder();
            WriteNode(node, sb, 0);
            return sb.ToString();
        }

        /// <summary>
        /// Writes a diff node as XML to a string builder.
        /// </summary>
        private static void WriteNode(DiffMatch node, StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent);

            // Use the element that exists (new or original)
            var element = node.NewElement ?? node.OriginalElement;
            if (element == null)
            {
                // Node was added or deleted - use a placeholder
                sb.Append($"{indentStr}<{node.Path?.Split('/').LastOrDefault() ?? "element"}>");
                return;
            }

            sb.Append($"{indentStr}<{element.Name}");

            // Add attributes if they changed
            if (node.Type == DiffType.Modified)
            {
                foreach (var attr in element.Attributes())
                {
                    sb.Append($" {attr.Name}=\"{attr.Value}\"");
                }
            }

            if (!element.HasElements && string.IsNullOrEmpty(element.Value))
            {
                sb.Append("/>");
            }
            else
            {
                sb.Append(">");

                if (!string.IsNullOrEmpty(element.Value))
                {
                    sb.Append(element.Value);
                }

                // Recursively write children
                foreach (var child in node.Children)
                {
                    WriteNode(child, sb, indent + 2);
                }

                sb.Append($"{indentStr}</{element.Name}>");
            }
        }
    }
}
