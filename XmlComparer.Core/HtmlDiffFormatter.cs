using System;
using System.Text;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates HTML formatted diff reports from XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This formatter provides HTML output for XML diffs. It wraps the
    /// <see cref="HtmlSideBySideFormatter"/> to provide a consistent
    /// <see cref="IDiffFormatter"/> interface.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new HtmlDiffFormatter();
    /// string html = formatter.Format(diffResult);
    /// File.WriteAllText("diff.html", html);
    /// </code>
    /// </example>
    public class HtmlDiffFormatter : IDiffFormatter
    {
        private readonly HtmlSideBySideFormatter _formatter;

        /// <summary>
        /// Creates a new HtmlDiffFormatter with default settings.
        /// </summary>
        public HtmlDiffFormatter()
        {
            _formatter = new HtmlSideBySideFormatter();
        }

        /// <summary>
        /// Formats a diff tree into an HTML string.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>An HTML formatted string.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into an HTML string with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>An HTML formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            string? embeddedJson = null;
            if (context?.EmbedJson == true && !string.IsNullOrEmpty(context.EmbeddedJson))
            {
                embeddedJson = context.EmbeddedJson;
            }

            return _formatter.GenerateHtml(diff, embeddedJson);
        }
    }
}
