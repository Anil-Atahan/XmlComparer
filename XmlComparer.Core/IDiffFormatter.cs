namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a contract for formatting diff results into various output formats.
    /// </summary>
    public interface IDiffFormatter
    {
        /// <summary>
        /// Formats a diff tree into a string representation.
        /// </summary>
        /// <param name="diff">The root of the diff tree to format.</param>
        /// <returns>A formatted string representation of the diff.</returns>
        string Format(DiffMatch diff);

        /// <summary>
        /// Formats a diff tree into a string representation with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree to format.</param>
        /// <param name="context">Additional context for formatting (e.g., validation results).</param>
        /// <returns>A formatted string representation of the diff.</returns>
        string Format(DiffMatch diff, FormatterContext context);
    }

    /// <summary>
    /// Provides context information for diff formatting.
    /// </summary>
    public class FormatterContext
    {
        /// <summary>
        /// Gets or sets the validation result to include in the output.
        /// </summary>
        public XmlValidationResult? ValidationResult { get; set; }

        /// <summary>
        /// Gets or sets whether to embed JSON data in HTML output.
        /// </summary>
        public bool EmbedJson { get; set; }

        /// <summary>
        /// Gets or sets custom JSON data to embed.
        /// </summary>
        public string? EmbeddedJson { get; set; }

        /// <summary>
        /// Creates a new formatter context.
        /// </summary>
        public FormatterContext() { }

        /// <summary>
        /// Creates a new formatter context with the specified values.
        /// </summary>
        public FormatterContext(XmlValidationResult? validationResult, bool embedJson = false, string? embeddedJson = null)
        {
            ValidationResult = validationResult;
            EmbedJson = embedJson;
            EmbeddedJson = embeddedJson;
        }
    }
}
