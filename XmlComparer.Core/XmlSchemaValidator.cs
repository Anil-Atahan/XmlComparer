using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace XmlComparer.Core
{
    /// <summary>
    /// Validates XML documents against XML Schema (XSD) definitions.
    /// </summary>
    public class XmlSchemaValidator
    {
        private readonly XmlSchemaSet _schemas;

        /// <summary>
        /// Create a validator from XSD file paths.
        /// </summary>
        /// <param name="xsdPaths">Paths to XSD schema files.</param>
        /// <exception cref="ArgumentNullException">Thrown when xsdPaths is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when schema compilation fails.</exception>
        public XmlSchemaValidator(IEnumerable<string> xsdPaths)
        {
            if (xsdPaths == null) throw new ArgumentNullException(nameof(xsdPaths));

            _schemas = new XmlSchemaSet();
            foreach (var path in xsdPaths)
            {
                // Validate path for traversal attempts
                XmlSecuritySettings.ValidatePath(path, nameof(xsdPaths));

                // Use secure settings when loading schemas
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                using var reader = XmlReader.Create(path, settings);
                var schema = XmlSchema.Read(reader, null);
                if (schema != null)
                {
                    _schemas.Add(schema);
                }
            }
            CompileSchemas();
        }

        /// <summary>
        /// Create a validator from a pre-built XmlSchemaSet (e.g., embedded resources).
        /// </summary>
        /// <param name="schemas">Pre-built XmlSchemaSet.</param>
        /// <exception cref="ArgumentNullException">Thrown when schemas is null.</exception>
        public XmlSchemaValidator(XmlSchemaSet schemas)
        {
            _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
            CompileSchemas();
        }

        /// <summary>
        /// Validate an XML file against the loaded schemas.
        /// </summary>
        /// <param name="xmlPath">Path to the XML file to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        public XmlValidationResult ValidateFile(string xmlPath)
        {
            XmlSecuritySettings.ValidatePath(xmlPath, nameof(xmlPath));

            var result = new XmlValidationResult();
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemas,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Exception != null)
                {
                    result.Errors.Add(new XmlValidationError(
                        SanitizeErrorMessage(e.Message),
                        e.Exception.LineNumber,
                        e.Exception.LinePosition));
                }
                else
                {
                    result.Errors.Add(new XmlValidationError(SanitizeErrorMessage(e.Message), 0, 0));
                }
            };

            using var reader = XmlReader.Create(xmlPath, settings);
            while (reader.Read()) { /* Read through document to trigger validation */ }
            return result;
        }

        /// <summary>
        /// Validate an XML file asynchronously against the loaded schemas.
        /// </summary>
        /// <param name="xmlPath">Path to the XML file to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with validation result.</returns>
        public async Task<XmlValidationResult> ValidateFileAsync(string xmlPath, CancellationToken cancellationToken = default)
        {
            XmlSecuritySettings.ValidatePath(xmlPath, nameof(xmlPath));

            var result = new XmlValidationResult();
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemas,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                Async = true
            };
            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Exception != null)
                {
                    result.Errors.Add(new XmlValidationError(
                        SanitizeErrorMessage(e.Message),
                        e.Exception.LineNumber,
                        e.Exception.LinePosition));
                }
                else
                {
                    result.Errors.Add(new XmlValidationError(SanitizeErrorMessage(e.Message), 0, 0));
                }
            };

            await using var stream = File.OpenRead(xmlPath);
            using var reader = XmlReader.Create(stream, settings);
            while (await reader.ReadAsync()) { /* Read through document to trigger validation */ }
            return result;
        }

        /// <summary>
        /// Validate XML content against the loaded schemas.
        /// </summary>
        /// <param name="xmlContent">XML content string to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        public XmlValidationResult ValidateContent(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));

            var result = new XmlValidationResult();
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemas,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Exception != null)
                {
                    result.Errors.Add(new XmlValidationError(
                        SanitizeErrorMessage(e.Message),
                        e.Exception.LineNumber,
                        e.Exception.LinePosition));
                }
                else
                {
                    result.Errors.Add(new XmlValidationError(SanitizeErrorMessage(e.Message), 0, 0));
                }
            };

            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (reader.Read()) { /* Read through document to trigger validation */ }
            return result;
        }

        /// <summary>
        /// Validate XML content asynchronously against the loaded schemas.
        /// </summary>
        /// <param name="xmlContent">XML content string to validate.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with validation result.</returns>
        public async Task<XmlValidationResult> ValidateContentAsync(string xmlContent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));

            var result = new XmlValidationResult();
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemas,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                Async = true
            };
            settings.ValidationEventHandler += (_, e) =>
            {
                if (e.Exception != null)
                {
                    result.Errors.Add(new XmlValidationError(
                        SanitizeErrorMessage(e.Message),
                        e.Exception.LineNumber,
                        e.Exception.LinePosition));
                }
                else
                {
                    result.Errors.Add(new XmlValidationError(SanitizeErrorMessage(e.Message), 0, 0));
                }
            };

            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (await reader.ReadAsync()) { /* Read through document to trigger validation */ }
            return result;
        }

        private void CompileSchemas()
        {
            try
            {
                _schemas.Compile();
            }
            catch (XmlSchemaException ex)
            {
                throw new InvalidOperationException("Schema compilation failed. Check that all XSD files are valid and properly referenced.", ex);
            }
        }

        /// <summary>
        /// Sanitizes error messages to prevent information disclosure.
        /// </summary>
        private static string SanitizeErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;

            // Remove absolute file paths from error messages
            var sanitized = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"[A-Z]:\\[^""'\r\n]*|/[^""'\r\n]*",
                "[REDACTED PATH]");

            return sanitized;
        }
    }
}
