using System;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates plain text formatted diff reports from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This formatter creates human-readable text reports showing the differences
    /// between XML documents in a simple, compact format.</para>
    /// <para>The text format shows:</para>
    /// <list type="bullet">
    ///   <item><description>Element path</description></item>
    ///   <description>Change type (Added, Deleted, Modified, Moved)</description>
    ///   <description>Element content</description>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new TextDiffFormatter();
    /// string text = formatter.Format(diffResult);
    /// Console.WriteLine(text);
    /// </code>
    /// </example>
    public class TextDiffFormatter : IDiffFormatter
    {
        private readonly StringBuilder _sb;
        private int _changeCount;

        /// <summary>
        /// Creates a new TextDiffFormatter.
        /// </summary>
        public TextDiffFormatter()
        {
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Formats a diff tree into a text string.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>A text formatted string.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into a text string with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>A text formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            _sb.Clear();
            _changeCount = 0;

            _sb.AppendLine("XML Comparison Report");
            _sb.AppendLine("======================");
            _sb.AppendLine();

            WriteDiffRecursive(diff, 0);

            _sb.AppendLine();
            _sb.AppendLine($"Total Changes: {_changeCount}");

            return _sb.ToString();
        }

        /// <summary>
        /// Writes diff information recursively.
        /// </summary>
        private void WriteDiffRecursive(DiffMatch node, int indent)
        {
            if (node.Type != DiffType.Unchanged)
            {
                _changeCount++;
                string indentStr = new string(' ', indent);

                string typeStr = node.Type switch
                {
                    DiffType.Added => "+",
                    DiffType.Deleted => "-",
                    DiffType.Modified => "~",
                    DiffType.Moved => ">",
                    DiffType.NamespaceChanged => "N",
                    _ => " "
                };

                _sb.Append($"{indentStr}{typeStr} {node.Path ?? "Unknown"}");

                if (node.Type == DiffType.Added && node.NewElement != null)
                {
                    _sb.AppendLine($" | {FormatElementBrief(node.NewElement)}");
                }
                else if (node.Type == DiffType.Deleted && node.OriginalElement != null)
                {
                    _sb.AppendLine($" | {FormatElementBrief(node.OriginalElement)}");
                }
                else if (node.Type == DiffType.Modified)
                {
                    string orig = node.OriginalElement != null ? FormatElementBrief(node.OriginalElement) : "(none)";
                    string newVal = node.NewElement != null ? FormatElementBrief(node.NewElement) : "(none)";
                    _sb.AppendLine($" | {orig} -> {newVal}");
                }
                else
                {
                    _sb.AppendLine();
                }
            }

            foreach (var child in node.Children)
            {
                WriteDiffRecursive(child, indent + 2);
            }
        }

        /// <summary>
        /// Formats an element briefly (name and key attributes only).
        /// </summary>
        private string FormatElementBrief(XElement element)
        {
            var brief = new StringBuilder();
            brief.Append($"<{element.Name}");

            // Add common identifying attributes
            string[] keyAttrs = { "id", "name", "key", "code" };
            foreach (var attr in keyAttrs)
            {
                var a = element.Attribute(attr);
                if (a != null)
                {
                    brief.Append($" {attr}=\"{a.Value}\"");
                    break;
                }
            }

            brief.Append(">");
            return brief.ToString();
        }
    }
}
