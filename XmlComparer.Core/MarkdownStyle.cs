using System;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines the markdown flavor/style for diff output.
    /// </summary>
    /// <remarks>
    /// <para>Different platforms support different markdown features. This enum allows
    /// you to choose the appropriate flavor for your target platform.</para>
    /// </remarks>
    public enum MarkdownFlavor
    {
        /// <summary>
        /// Standard CommonMark markdown with basic code blocks.
        /// </summary>
        Standard,

        /// <summary>
        /// GitHub Flavored Markdown (GFM) with syntax highlighting and task lists.
        /// </summary>
        /// <remarks>
        /// Supports fenced code blocks with language specifiers, task lists,
        /// tables, strikethrough, and autolinks.
        /// </remarks>
        GitHub,

        /// <summary>
        /// GitLab Flavored Markdown (GLFM) with math and mermaid support.
        /// </summary>
        /// <remarks>
        /// Supports all GFM features plus math blocks ($$), mermaid diagrams,
        /// and PlantUML.
        /// </remarks>
        GitLab,

        /// <summary>
        /// Bitbucket markdown with Jira integration.
        /// </summary>
        /// <remarks>
        /// Supports code blocks with language specifiers, macros,
        /// and Jira issue linking.
        /// </remarks>
        Bitbucket
    }

    /// <summary>
    /// Defines the detail level for markdown diff output.
    /// </summary>
    public enum MarkdownDetailLevel
    {
        /// <summary>
        /// Shows only summary statistics and file-level changes.
        /// </summary>
        Summary,

        /// <summary>
        /// Shows all changes with full XML snippets.
        /// </summary>
        Full,

        /// <summary>
        /// Shows changes with paths only, no content.
        /// </summary>
        Compact
    }

    /// <summary>
    /// Configuration options for markdown diff formatting.
    /// </summary>
    /// <remarks>
    /// <para>This class controls how XML diffs are rendered as markdown.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var style = new MarkdownStyle
    /// {
    ///     Flavor = MarkdownFlavor.GitHub,
    ///     DetailLevel = MarkdownDetailLevel.Full,
    ///     IncludeTableOfContents = true,
    ///     IncludeStatistics = true
    /// };
    ///
    /// var formatter = new MarkdownDiffFormatter(style);
    /// string markdown = formatter.Format(diffResult);
    /// </code>
    /// </example>
    public class MarkdownStyle
    {
        /// <summary>
        /// Gets or sets the markdown flavor to use.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="MarkdownFlavor.GitHub"/>.
        /// </remarks>
        public MarkdownFlavor Flavor { get; set; } = MarkdownFlavor.GitHub;

        /// <summary>
        /// Gets or sets the detail level for the output.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="MarkdownDetailLevel.Full"/>.
        /// </remarks>
        public MarkdownDetailLevel DetailLevel { get; set; } = MarkdownDetailLevel.Full;

        /// <summary>
        /// Gets or sets whether to include a table of contents.
        /// </summary>
        /// <remarks>
        /// When true, generates a TOC at the top of the document with links to each section.
        /// </remarks>
        public bool IncludeTableOfContents { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to include diff statistics.
        /// </summary>
        /// <remarks>
        /// When true, includes counts of additions, deletions, and modifications.
        /// </remarks>
        public bool IncludeStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include timestamps in the output.
        /// </summary>
        public bool IncludeTimestamp { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum depth for nested XML elements.
        /// </summary>
        /// <remarks>
        /// Elements deeper than this depth will be truncated with "...".
        /// Set to 0 for unlimited depth.
        /// </remarks>
        public int MaxDepth { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to use emoji for change indicators.
        /// </summary>
        /// <remarks>
        /// When true, uses emoji like ➕, ➖, 🔄 instead of text indicators.
        /// </remarks>
        public bool UseEmoji { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to group changes by type.
        /// </summary>
        /// <remarks>
        /// When true, creates separate sections for additions, deletions, and modifications.
        /// </remarks>
        public bool GroupByChangeType { get; set; } = false;

        /// <summary>
        /// Gets or sets the title for the markdown document.
        /// </summary>
        /// <remarks>
        /// Default is "XML Comparison Report".
        /// </remarks>
        public string Title { get; set; } = "XML Comparison Report";

        /// <summary>
        /// Gets or sets the subtitle for the markdown document.
        /// </summary>
        public string? Subtitle { get; set; }

        /// <summary>
        /// Creates a new MarkdownStyle with GitHub-flavored markdown settings.
        /// </summary>
        public MarkdownStyle() { }

        /// <summary>
        /// Creates a new MarkdownStyle with the specified flavor.
        /// </summary>
        /// <param name="flavor">The markdown flavor to use.</param>
        public MarkdownStyle(MarkdownFlavor flavor)
        {
            Flavor = flavor;
        }

        /// <summary>
        /// Gets the code fence language identifier for this flavor.
        /// </summary>
        /// <returns>The language identifier (e.g., "xml" for most flavors).</returns>
        public string GetCodeLanguage()
        {
            return Flavor switch
            {
                MarkdownFlavor.GitHub => "xml",
                MarkdownFlavor.GitLab => "xml",
                MarkdownFlavor.Bitbucket => "xml",
                _ => "xml"
            };
        }

        /// <summary>
        /// Gets the task list syntax for this flavor.
        /// </summary>
        /// <param name="checked">Whether the task is checked.</param>
        /// <returns>The markdown task list item.</returns>
        public string GetTaskItem(bool isChecked)
        {
            return isChecked ? "- [x]" : "- [ ]";
        }

        /// <summary>
        /// Creates a standard markdown style with minimal formatting.
        /// </summary>
        /// <returns>A new MarkdownStyle configured for standard markdown.</returns>
        public static MarkdownStyle Standard() => new MarkdownStyle(MarkdownFlavor.Standard);

        /// <summary>
        /// Creates a GitHub-flavored markdown style with all features enabled.
        /// </summary>
        /// <returns>A new MarkdownStyle configured for GitHub.</returns>
        public static MarkdownStyle GitHub() => new MarkdownStyle(MarkdownFlavor.GitHub)
        {
            IncludeStatistics = true,
            IncludeTableOfContents = true,
            UseEmoji = true
        };

        /// <summary>
        /// Creates a GitLab-flavored markdown style with all features enabled.
        /// </summary>
        /// <returns>A new MarkdownStyle configured for GitLab.</returns>
        public static MarkdownStyle GitLab() => new MarkdownStyle(MarkdownFlavor.GitLab)
        {
            IncludeStatistics = true,
            IncludeTableOfContents = true,
            UseEmoji = true
        };

        /// <summary>
        /// Creates a Bitbucket-flavored markdown style.
        /// </summary>
        /// <returns>A new MarkdownStyle configured for Bitbucket.</returns>
        public static MarkdownStyle Bitbucket() => new MarkdownStyle(MarkdownFlavor.Bitbucket);

        /// <summary>
        /// Creates a compact markdown style for brief summaries.
        /// </summary>
        /// <returns>A new MarkdownStyle configured for compact output.</returns>
        public static MarkdownStyle Compact() => new MarkdownStyle
        {
            DetailLevel = MarkdownDetailLevel.Compact,
            IncludeStatistics = true,
            IncludeTableOfContents = false,
            GroupByChangeType = true
        };
    }
}
