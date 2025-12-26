using System.Threading;
using System.Threading.Tasks;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a contract for validating XML documents against schemas.
    /// </summary>
    public interface IXmlValidator
    {
        /// <summary>
        /// Validates an XML file against the loaded schemas.
        /// </summary>
        /// <param name="xmlPath">Path to the XML file to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        XmlValidationResult ValidateFile(string xmlPath);

        /// <summary>
        /// Validates an XML file asynchronously against the loaded schemas.
        /// </summary>
        /// <param name="xmlPath">Path to the XML file to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with validation result.</returns>
        Task<XmlValidationResult> ValidateFileAsync(string xmlPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates XML content against the loaded schemas.
        /// </summary>
        /// <param name="xmlContent">XML content string to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        XmlValidationResult ValidateContent(string xmlContent);

        /// <summary>
        /// Validates XML content asynchronously against the loaded schemas.
        /// </summary>
        /// <param name="xmlContent">XML content string to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with validation result.</returns>
        Task<XmlValidationResult> ValidateContentAsync(string xmlContent, CancellationToken cancellationToken = default);
    }
}
