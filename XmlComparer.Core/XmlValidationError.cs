namespace XmlComparer.Core
{
    /// <summary>
    /// Represents a single XSD schema validation error.
    /// </summary>
    /// <remarks>
    /// This class encapsulates information about a validation error found during
    /// XSD schema validation, including the error message and its location in the source document.
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var error in validationResult.Errors)
    /// {
    ///     Console.WriteLine($"Error at ({error.LineNumber},{error.LinePosition}): {error.Message}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="XmlValidationResult"/>
    public class XmlValidationError
    {
        /// <summary>
        /// Creates a new validation error with the specified details.
        /// </summary>
        /// <param name="message">The validation error message.</param>
        /// <param name="lineNumber">The 1-based line number where the error occurred.</param>
        /// <param name="linePosition">The 1-based column position where the error occurred.</param>
        /// <example>
        /// <code>
        /// var error = new XmlValidationError("Element 'foo' is not declared.", 42, 5);
        /// </code>
        /// </example>
        public XmlValidationError(string message, int lineNumber, int linePosition)
        {
            Message = message;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        /// <remarks>
        /// This message typically describes what constraint was violated, such as
        /// "The 'Product' element has invalid child element 'Price'. Expected 'Name'."
        /// </remarks>
        public string Message { get; }

        /// <summary>
        /// Gets the 1-based line number where the validation error occurred.
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine($"Error on line {error.LineNumber}");
        /// </code>
        /// </example>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the 1-based column position where the validation error occurred.
        /// </summary>
        /// <example>
        /// <code>
        /// Console.WriteLine($"Error at column {error.LinePosition}");
        /// </code>
        /// </example>
        public int LinePosition { get; }
    }
}
