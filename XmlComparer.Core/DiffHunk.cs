using System.Collections.Generic;
using System.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Represents a hunk (a contiguous section of changes) in a unified diff format.
    /// </summary>
    /// <remarks>
    /// <para>In unified diff format, changes are organized into hunks. Each hunk represents
    /// a section of the file where changes occur, with context lines before and after.</para>
    /// <para>The hunk header specifies the line ranges in both original and new files:</para>
    /// <code>
    /// @@ -originalStart,originalCount +newStart,newCount @@
    /// </code>
    /// </remarks>
    /// <example>
    /// <code>
    /// var hunk = new DiffHunk
    /// {
    ///     OriginalStart = 10,
    ///     OriginalCount = 5,
    ///     NewStart = 10,
    ///     NewCount = 6,
    ///     Lines = new List&lt;DiffLine&gt;
    /// };
    /// // Header: @@ -10,5 +10,6 @@
    /// </code>
    /// </example>
    public class DiffHunk
    {
        /// <summary>
        /// Gets or sets the starting line number in the original file (1-indexed).
        /// </summary>
        public int OriginalStart { get; set; }

        /// <summary>
        /// Gets or sets the number of lines from the original file in this hunk.
        /// </summary>
        public int OriginalCount { get; set; }

        /// <summary>
        /// Gets or sets the starting line number in the new file (1-indexed).
        /// </summary>
        public int NewStart { get; set; }

        /// <summary>
        /// Gets or sets the number of lines from the new file in this hunk.
        /// </summary>
        public int NewCount { get; set; }

        /// <summary>
        /// Gets or sets the list of diff lines in this hunk.
        /// </summary>
        /// <remarks>
        /// Each line is prefixed with '+' (added), '-' (removed), or ' ' (context).
        /// </remarks>
        public List<DiffLine> Lines { get; set; } = new List<DiffLine>();

        /// <summary>
        /// Formats the hunk header line.
        /// </summary>
        /// <returns>A string in the format "@@ -originalStart,originalCount +newStart,newCount @@"</returns>
        /// <example>
        /// <code>
        /// hunk.ToHeader(); // "@@ -10,5 +10,6 @@"
        /// </code>
        /// </example>
        public string ToHeader()
        {
            return $"@@ -{OriginalStart},{OriginalCount} +{NewStart},{NewCount} @@";
        }

        /// <summary>
        /// Gets all lines in the hunk as formatted strings.
        /// </summary>
        /// <returns>An enumerable of formatted diff lines.</returns>
        public IEnumerable<string> GetFormattedLines()
        {
            yield return ToHeader();
            foreach (var line in Lines)
            {
                yield return line.ToString();
            }
        }

        /// <summary>
        /// Adds a context line (present in both files).
        /// </summary>
        /// <param name="content">The line content (without leading space).</param>
        /// <param name="originalLine">The line number in the original file.</param>
        /// <param name="newLine">The line number in the new file.</param>
        public void AddContext(string content, int originalLine, int newLine)
        {
            Lines.Add(new DiffLine(DiffLineType.Context, content, originalLine, newLine));
        }

        /// <summary>
        /// Adds a deletion line (removed from original file).
        /// </summary>
        /// <param name="content">The line content (without leading minus).</param>
        /// <param name="originalLine">The line number in the original file.</param>
        public void AddDeletion(string content, int originalLine)
        {
            Lines.Add(new DiffLine(DiffLineType.Deletion, content, originalLine, null));
        }

        /// <summary>
        /// Adds an addition line (added to new file).
        /// </summary>
        /// <param name="content">The line content (without leading plus).</param>
        /// <param name="newLine">The line number in the new file.</param>
        public void AddAddition(string content, int newLine)
        {
            Lines.Add(new DiffLine(DiffLineType.Addition, content, null, newLine));
        }
    }

    /// <summary>
    /// Represents a single line in a unified diff.
    /// </summary>
    public class DiffLine
    {
        /// <summary>
        /// Gets or sets the type of diff line.
        /// </summary>
        public DiffLineType Type { get; set; }

        /// <summary>
        /// Gets or sets the line content (without the prefix character).
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number in the original file, or null if not applicable.
        /// </summary>
        public int? OriginalLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the line number in the new file, or null if not applicable.
        /// </summary>
        public int? NewLineNumber { get; set; }

        /// <summary>
        /// Creates a new DiffLine.
        /// </summary>
        public DiffLine() { }

        /// <summary>
        /// Creates a new DiffLine with all parameters.
        /// </summary>
        /// <param name="type">The type of diff line.</param>
        /// <param name="content">The line content.</param>
        /// <param name="originalLineNumber">The line number in the original file.</param>
        /// <param name="newLineNumber">The line number in the new file.</param>
        public DiffLine(DiffLineType type, string content, int? originalLineNumber, int? newLineNumber)
        {
            Type = type;
            Content = content;
            OriginalLineNumber = originalLineNumber;
            NewLineNumber = newLineNumber;
        }

        /// <summary>
        /// Formats the line as a unified diff string.
        /// </summary>
        /// <returns>A string with the appropriate prefix character (+, -, or space).</returns>
        /// <example>
        /// <code>
        /// var line = new DiffLine(DiffLineType.Addition, "New line", null, 42);
        /// line.ToString(); // "+New line"
        /// </code>
        /// </example>
        public override string ToString()
        {
            char prefix = Type switch
            {
                DiffLineType.Addition => '+',
                DiffLineType.Deletion => '-',
                DiffLineType.Context => ' ',
                _ => ' '
            };
            return $"{prefix}{Content}";
        }
    }

    /// <summary>
    /// Defines the type of line in a unified diff.
    /// </summary>
    public enum DiffLineType
    {
        /// <summary>
        /// Context line (present in both files, prefixed with space).
        /// </summary>
        Context,

        /// <summary>
        /// Addition (added to new file, prefixed with +).
        /// </summary>
        Addition,

        /// <summary>
        /// Deletion (removed from original file, prefixed with -).
        /// </summary>
        Deletion
    }
}
