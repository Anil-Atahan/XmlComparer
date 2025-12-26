using System.Collections.Generic;

namespace XmlComparer.Core
{
    /// <summary>
    /// Contains the results of XSD schema validation for one or two XML documents.
    /// </summary>
    /// <remarks>
    /// <para>This class encapsulates validation errors found during XSD schema validation.
    /// Validation is performed via <see cref="XmlComparerService.ValidateXmlFiles"/> or
    /// <see cref="XmlComparerService.ValidateXmlContent"/> when XSD schemas are specified
    /// through <see cref="XmlComparisonOptions.ValidateWithXsds(string[])"/>.</para>
    /// <para>The validation checks both the original and new XML documents for conformance
    /// to the specified XSD schemas. Any errors found are collected in the <see cref="Errors"/> list.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = XmlComparer.CompareFilesWithReport("old.xml", "new.xml",
    ///     options => options.ValidateWithXsds("schema.xsd"));
    ///
    /// if (result.Validation != null)
    /// {
    ///     if (result.Validation.IsValid)
    ///     {
    ///         Console.WriteLine("Both documents are valid.");
    ///     }
    ///     else
    ///     {
    ///         Console.WriteLine($"Validation failed with {result.Validation.Errors.Count} errors:");
    ///         foreach (var error in result.Validation.Errors)
    ///         {
    ///             Console.WriteLine($"  Line {error.LineNumber}, Col {error.LinePosition}: {error.Message}");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="XmlValidationError"/>
    /// <seealso cref="XmlSchemaValidator"/>
    public class XmlValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether both XML documents passed XSD validation.
        /// </summary>
        /// <remarks>
        /// Returns true if no validation errors were found; false if <see cref="Errors"/> contains any entries.
        /// </remarks>
        /// <example>
        /// <code>
        /// if (validationResult.IsValid)
        ///     Console.WriteLine("Documents are valid according to the schema.");
        /// </code>
        /// </example>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// Gets the list of validation errors found during schema validation.
        /// </summary>
        /// <remarks>
        /// This list contains errors from both the original and new XML documents.
        /// Each error includes a message, line number, and position for easy debugging.
        /// </remarks>
        /// <example>
        /// <code>
        /// foreach (var error in validationResult.Errors)
        /// {
        ///     Console.WriteLine($"Error at line {error.LineNumber}: {error.Message}");
        /// }
        /// </code>
        /// </example>
        public List<XmlValidationError> Errors { get; } = new List<XmlValidationError>();
    }
}
