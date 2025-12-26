using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates JSON representations of XML diff results.
    /// </summary>
    /// <remarks>
    /// <para>This class converts the <see cref="DiffMatch"/> tree structure into a JSON format
    /// suitable for programmatic consumption, API responses, or data interchange.</para>
    /// <para>The JSON output includes:</para>
    /// <list type="bullet">
    ///   <item><description><b>Diff</b> - The hierarchical diff tree with node names, paths, types, attributes, and values</description></item>
    ///   <item><description><b>Summary</b> - Counts of each diff type (total, unchanged, added, deleted, modified, moved)</description></item>
    ///   <item><description><b>Validation</b> - Optional XSD validation results</description></item>
    /// </list>
    /// <para>JSON output respects the <see cref="XmlDiffConfig"/> settings such as excluded nodes/attributes
    /// and value ignoring, ensuring the JSON only includes relevant comparison data.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new XmlDiffConfig { KeyAttributeNames = { "id" } };
    /// var formatter = new JsonDiffFormatter(config);
    ///
    /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
    /// string json = formatter.GenerateJson(diff);
    ///
    /// // With validation
    /// var result = XmlComparer.CompareFilesWithReport("old.xml", "new.xml",
    ///     options => options.ValidateWithXsds("schema.xsd").IncludeJson());
    /// // json is available in result.Json
    /// </code>
    /// </example>
    public class JsonDiffFormatter : IDiffFormatter
    {
        private readonly XmlDiffConfig _config;
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Creates a new JSON formatter with a default configuration.
        /// </summary>
        /// <remarks>
        /// Uses a default <see cref="XmlDiffConfig"/> with no exclusions or value ignoring.
        /// </remarks>
        public JsonDiffFormatter() : this(new XmlDiffConfig()) { }

        /// <summary>
        /// Creates a new JSON formatter with the specified configuration.
        /// </summary>
        /// <param name="config">The diff configuration that controls filtering and content inclusion.</param>
        /// <remarks>
        /// The configuration determines which nodes and attributes are excluded from the JSON output.
        /// </remarks>
        public JsonDiffFormatter(XmlDiffConfig config)
        {
            _config = config;
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _options.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        /// Formats a diff tree into a JSON string.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <returns>A JSON formatted string.</returns>
        public string Format(DiffMatch diff)
        {
            return Format(diff, new FormatterContext());
        }

        /// <summary>
        /// Formats a diff tree into a JSON string with additional context.
        /// </summary>
        /// <param name="diff">The root of the diff tree.</param>
        /// <param name="context">Additional formatting context.</param>
        /// <returns>A JSON formatted string.</returns>
        public string Format(DiffMatch diff, FormatterContext context)
        {
            if (context?.ValidationResult != null)
            {
                return GenerateJson(diff, context.ValidationResult);
            }
            return GenerateJson(diff);
        }

        /// <summary>
        /// Generates a JSON document representing the diff tree and summary.
        /// </summary>
        /// <param name="root">The root of the diff tree.</param>
        /// <returns>A JSON string containing the diff and summary.</returns>
        /// <example>
        /// <code>
        /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
        /// var formatter = new JsonDiffFormatter(new XmlDiffConfig());
        /// string json = formatter.GenerateJson(diff);
        /// </code>
        /// </example>
        public string GenerateJson(DiffMatch root)
        {
            var dto = new DiffDocumentDto
            {
                // Stable envelope shape for consumers.
                Diff = BuildNode(root) ?? new DiffNodeDto(),
                Summary = DiffSummaryCalculator.Compute(root)
            };
            return JsonSerializer.Serialize(dto, _options);
        }

        /// <summary>
        /// Generates a JSON document representing the diff tree, summary, and validation results.
        /// </summary>
        /// <param name="root">The root of the diff tree.</param>
        /// <param name="validationResult">The XSD validation results.</param>
        /// <returns>A JSON string containing the diff, summary, and validation.</returns>
        /// <example>
        /// <code>
        /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
        /// var validation = new XmlSchemaValidator().ValidateFile("new.xml");
        /// var formatter = new JsonDiffFormatter(new XmlDiffConfig());
        /// string json = formatter.GenerateJson(diff, validation);
        /// </code>
        /// </example>
        public string GenerateJson(DiffMatch root, XmlValidationResult validationResult)
        {
            var dto = new DiffDocumentDto
            {
                Validation = BuildValidation(validationResult),
                Diff = BuildNode(root) ?? new DiffNodeDto(),
                Summary = DiffSummaryCalculator.Compute(root)
            };
            return JsonSerializer.Serialize(dto, _options);
        }

        /// <summary>
        /// Generates a JSON document containing only validation results.
        /// </summary>
        /// <param name="validationResult">The XSD validation results.</param>
        /// <returns>A JSON string containing only the validation data.</returns>
        /// <example>
        /// <code>
        /// var validator = new XmlSchemaValidator(schemaSet);
        /// var result = validator.ValidateFile("data.xml");
        /// var formatter = new JsonDiffFormatter(new XmlDiffConfig());
        /// string json = formatter.GenerateValidationJson(result);
        /// </code>
        /// </example>
        public string GenerateValidationJson(XmlValidationResult validationResult)
        {
            var dto = new DiffDocumentDto
            {
                Validation = BuildValidation(validationResult)
            };
            return JsonSerializer.Serialize(dto, _options);
        }

        /// <summary>
        /// Builds a JSON node DTO from a diff match node.
        /// </summary>
        /// <param name="match">The diff match node.</param>
        /// <returns>A JSON node DTO, or null if the node is excluded.</returns>
        /// <remarks>
        /// Excluded nodes are filtered based on <see cref="XmlDiffConfig.ExcludedNodeNames"/>
        /// and <see cref="XmlDiffConfig.ExcludeSubtree"/>.
        /// </remarks>
        private DiffNodeDto? BuildNode(DiffMatch match)
        {
            bool isExcluded = IsExcluded(match.OriginalElement) || IsExcluded(match.NewElement);
            if (isExcluded && _config.ExcludeSubtree)
            {
                return null;
            }

            var node = new DiffNodeDto
            {
                Name = FormatName(match.OriginalElement?.Name ?? match.NewElement?.Name),
                Path = match.Path,
                Type = match.Type
            };

            if (!isExcluded && match.OriginalElement != null)
            {
                node.OriginalAttributes = GetAttributes(match.OriginalElement);
                if (!_config.IgnoreValues)
                {
                    node.OriginalValue = GetValueIfLeaf(match.OriginalElement);
                }
            }

            if (!isExcluded && match.NewElement != null)
            {
                node.NewAttributes = GetAttributes(match.NewElement);
                if (!_config.IgnoreValues)
                {
                    node.NewValue = GetValueIfLeaf(match.NewElement);
                }
            }

            if (match.Children.Count > 0)
            {
                var children = match.Children.Select(BuildNode).Where(c => c != null).ToList();
                if (children.Count > 0)
                {
                    node.Children = children!;
                }
            }

            return node;
        }

        /// <summary>
        /// Builds a validation result DTO.
        /// </summary>
        private ValidationDto BuildValidation(XmlValidationResult validationResult)
        {
            return new ValidationDto
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors
                    .Select(e => new ValidationErrorDto
                    {
                        Message = e.Message,
                        LineNumber = e.LineNumber,
                        LinePosition = e.LinePosition
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Extracts non-excluded attributes from an element.
        /// </summary>
        private Dictionary<string, string>? GetAttributes(XElement element)
        {
            var attrs = element.Attributes()
                .Where(a => !_config.ExcludedAttributeNames.Contains(a.Name.LocalName))
                .ToDictionary(a => a.Name.LocalName, a => a.Value);

            return attrs.Count > 0 ? attrs : null;
        }

        /// <summary>
        /// Gets the text value of a leaf element (one with no child elements).
        /// </summary>
        private string? GetValueIfLeaf(XElement element)
        {
            if (element.HasElements || string.IsNullOrEmpty(element.Value)) return null;
            return element.Value;
        }

        /// <summary>
        /// Checks if an element is excluded from the output.
        /// </summary>
        private bool IsExcluded(XElement? element)
        {
            return element != null && _config.ExcludedNodeNames.Contains(element.Name.LocalName);
        }

        /// <summary>
        /// Formats an XML name as a string, including namespace if present.
        /// </summary>
        private static string FormatName(XName? name)
        {
            if (name == null) return string.Empty;
            if (name.Namespace == XNamespace.None) return name.LocalName;
            return $"{{{name.NamespaceName}}}{name.LocalName}";
        }

        /// <summary>
        /// Internal DTO for diff nodes in JSON output.
        /// </summary>
        private class DiffNodeDto
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public DiffType Type { get; set; }
            public Dictionary<string, string>? OriginalAttributes { get; set; }
            public Dictionary<string, string>? NewAttributes { get; set; }
            public string? OriginalValue { get; set; }
            public string? NewValue { get; set; }
            public List<DiffNodeDto>? Children { get; set; }
        }

        /// <summary>
        /// Internal DTO for the complete JSON document.
        /// </summary>
        private class DiffDocumentDto
        {
            public ValidationDto? Validation { get; set; }
            public DiffNodeDto? Diff { get; set; }
            public DiffSummary? Summary { get; set; }
        }

        /// <summary>
        /// Internal DTO for validation results.
        /// </summary>
        private class ValidationDto
        {
            public bool IsValid { get; set; }
            public List<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
        }

        /// <summary>
        /// Internal DTO for validation errors.
        /// </summary>
        private class ValidationErrorDto
        {
            public string Message { get; set; } = string.Empty;
            public int LineNumber { get; set; }
            public int LinePosition { get; set; }
        }
    }
}
