using System;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace XmlComparer.Core
{
    /// <summary>
    /// Main service for XML comparison operations with validation and reporting capabilities.
    /// </summary>
    public class XmlComparerService
    {
        private readonly XmlDiffConfig _config;
        private readonly IMatchingStrategy _strategy;

        /// <summary>
        /// Creates a new XmlComparerService instance.
        /// </summary>
        /// <param name="config">Configuration options for XML comparison.</param>
        /// <param name="strategy">Optional custom matching strategy for element comparison.</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public XmlComparerService(XmlDiffConfig config, IMatchingStrategy? strategy = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _strategy = strategy ?? new DefaultMatchingStrategy();
        }

        /// <summary>
        /// Compare two XML files on disk.
        /// </summary>
        /// <param name="originalXmlPath">Path to the original XML file.</param>
        /// <param name="newXmlPath">Path to the new XML file.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences.</exception>
        /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when file size exceeds maximum allowed.</exception>
        public DiffMatch Compare(string originalXmlPath, string newXmlPath)
        {
            XmlSecuritySettings.ValidatePath(originalXmlPath, nameof(originalXmlPath));
            XmlSecuritySettings.ValidatePath(newXmlPath, nameof(newXmlPath));
            XmlSecuritySettings.ValidateFileSize(originalXmlPath);
            XmlSecuritySettings.ValidateFileSize(newXmlPath);

            var originalDoc = LoadAndClean(originalXmlPath);
            var newDoc = LoadAndClean(newXmlPath);

            var engine = new XmlDiffEngine(_config, _strategy);
            return engine.Compare(originalDoc.Root, newDoc.Root);
        }

        /// <summary>
        /// Compare two XML file streams.
        /// </summary>
        /// <param name="originalStream">Stream containing the original XML.</param>
        /// <param name="newStream">Stream containing the new XML.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either stream is null.</exception>
        public DiffMatch Compare(Stream originalStream, Stream newStream)
        {
            if (originalStream == null) throw new ArgumentNullException(nameof(originalStream));
            if (newStream == null) throw new ArgumentNullException(nameof(newStream));

            var originalDoc = LoadAndClean(originalStream);
            var newDoc = LoadAndClean(newStream);

            var engine = new XmlDiffEngine(_config, _strategy);
            return engine.Compare(originalDoc.Root, newDoc.Root);
        }

        /// <summary>
        /// Compare two XML files asynchronously using true async I/O.
        /// </summary>
        /// <param name="originalXmlPath">Path to the original XML file.</param>
        /// <param name="newXmlPath">Path to the new XML file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with DiffMatch result.</returns>
        public async Task<DiffMatch> CompareAsync(string originalXmlPath, string newXmlPath, CancellationToken cancellationToken = default)
        {
            XmlSecuritySettings.ValidatePath(originalXmlPath, nameof(originalXmlPath));
            XmlSecuritySettings.ValidatePath(newXmlPath, nameof(newXmlPath));
            XmlSecuritySettings.ValidateFileSize(originalXmlPath);
            XmlSecuritySettings.ValidateFileSize(newXmlPath);

            var settings = XmlSecuritySettings.CreateSecureReaderSettings();
            settings.Async = true;

#if NETSTANDARD2_0
            using var originalStream = File.OpenRead(originalXmlPath);
            using var newStream = File.OpenRead(newXmlPath);

            using var originalReader = XmlReader.Create(originalStream, settings);
            using var newReader = XmlReader.Create(newStream, settings);

            var originalDoc = XDocument.Load(originalReader, LoadOptions.None);
            var newDoc = XDocument.Load(newReader, LoadOptions.None);
#else
            await using var originalStream = File.OpenRead(originalXmlPath);
            await using var newStream = File.OpenRead(newXmlPath);

            using var originalReader = XmlReader.Create(originalStream, settings);
            using var newReader = XmlReader.Create(newStream, settings);

            var originalDoc = await XDocument.LoadAsync(originalReader, LoadOptions.None, cancellationToken);
            var newDoc = await XDocument.LoadAsync(newReader, LoadOptions.None, cancellationToken);
#endif

            CleanUpDocument(originalDoc);
            CleanUpDocument(newDoc);

            var engine = new XmlDiffEngine(_config, _strategy);
            return engine.Compare(originalDoc.Root, newDoc.Root);
        }

        /// <summary>
        /// Compare two XML strings.
        /// </summary>
        /// <param name="originalXmlContent">Original XML content string.</param>
        /// <param name="newXmlContent">New XML content string.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either content is null or empty.</exception>
        /// <exception cref="XmlException">Thrown when content contains malformed XML.</exception>
        public DiffMatch CompareXml(string originalXmlContent, string newXmlContent)
        {
            if (string.IsNullOrWhiteSpace(originalXmlContent))
                throw new ArgumentException("Original XML content cannot be null or empty.", nameof(originalXmlContent));
            if (string.IsNullOrWhiteSpace(newXmlContent))
                throw new ArgumentException("New XML content cannot be null or empty.", nameof(newXmlContent));

            var originalDoc = ParseAndClean(originalXmlContent);
            var newDoc = ParseAndClean(newXmlContent);

            var engine = new XmlDiffEngine(_config, _strategy);
            return engine.Compare(originalDoc.Root, newDoc.Root);
        }

        /// <summary>
        /// Compare two XML strings asynchronously.
        /// </summary>
        /// <param name="originalXmlContent">Original XML content string.</param>
        /// <param name="newXmlContent">New XML content string.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with DiffMatch result.</returns>
        public async Task<DiffMatch> CompareXmlAsync(string originalXmlContent, string newXmlContent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(originalXmlContent))
                throw new ArgumentException("Original XML content cannot be null or empty.", nameof(originalXmlContent));
            if (string.IsNullOrWhiteSpace(newXmlContent))
                throw new ArgumentException("New XML content cannot be null or empty.", nameof(newXmlContent));

            var settings = XmlSecuritySettings.CreateSecureReaderSettings();
            settings.Async = true;

            using var originalReader = XmlReader.Create(new StringReader(originalXmlContent), settings);
            using var newReader = XmlReader.Create(new StringReader(newXmlContent), settings);

#if NETSTANDARD2_0
            var originalDoc = XDocument.Load(originalReader, LoadOptions.None);
            var newDoc = XDocument.Load(newReader, LoadOptions.None);
#else
            var originalDoc = await XDocument.LoadAsync(originalReader, LoadOptions.None, cancellationToken);
            var newDoc = await XDocument.LoadAsync(newReader, LoadOptions.None, cancellationToken);
#endif

            CleanUpDocument(originalDoc);
            CleanUpDocument(newDoc);

            var engine = new XmlDiffEngine(_config, _strategy);
            return engine.Compare(originalDoc.Root, newDoc.Root);
        }

        /// <summary>
        /// Validate both XML files against one or more XSDs.
        /// </summary>
        public XmlValidationResult ValidateXmlFiles(IEnumerable<string> xsdPaths, string originalXmlPath, string newXmlPath)
        {
            var validator = new XmlSchemaValidator(xsdPaths);
            var result = new XmlValidationResult();
            var originalResult = validator.ValidateFile(originalXmlPath);
            var newResult = validator.ValidateFile(newXmlPath);

            result.Errors.AddRange(originalResult.Errors);
            result.Errors.AddRange(newResult.Errors);
            return result;
        }

        /// <summary>
        /// Validate both XML files against one or more XSDs asynchronously.
        /// </summary>
        public async Task<XmlValidationResult> ValidateXmlFilesAsync(IEnumerable<string> xsdPaths, string originalXmlPath, string newXmlPath, CancellationToken cancellationToken = default)
        {
            var validator = new XmlSchemaValidator(xsdPaths);
            var result = new XmlValidationResult();
            var originalResult = await validator.ValidateFileAsync(originalXmlPath, cancellationToken).ConfigureAwait(false);
            var newResult = await validator.ValidateFileAsync(newXmlPath, cancellationToken).ConfigureAwait(false);

            result.Errors.AddRange(originalResult.Errors);
            result.Errors.AddRange(newResult.Errors);
            return result;
        }

        /// <summary>
        /// Validate both XML strings against one or more XSDs.
        /// </summary>
        public XmlValidationResult ValidateXmlContent(IEnumerable<string> xsdPaths, string originalXmlContent, string newXmlContent)
        {
            var validator = new XmlSchemaValidator(xsdPaths);
            var result = new XmlValidationResult();
            var originalResult = validator.ValidateContent(originalXmlContent);
            var newResult = validator.ValidateContent(newXmlContent);

            result.Errors.AddRange(originalResult.Errors);
            result.Errors.AddRange(newResult.Errors);
            return result;
        }

        /// <summary>
        /// Validate both XML strings against one or more XSDs asynchronously.
        /// </summary>
        public async Task<XmlValidationResult> ValidateXmlContentAsync(IEnumerable<string> xsdPaths, string originalXmlContent, string newXmlContent, CancellationToken cancellationToken = default)
        {
            var validator = new XmlSchemaValidator(xsdPaths);
            var result = new XmlValidationResult();
            var originalResult = await validator.ValidateContentAsync(originalXmlContent, cancellationToken).ConfigureAwait(false);
            var newResult = await validator.ValidateContentAsync(newXmlContent, cancellationToken).ConfigureAwait(false);

            result.Errors.AddRange(originalResult.Errors);
            result.Errors.AddRange(newResult.Errors);
            return result;
        }

        /// <summary>
        /// Loads an XML document from file with security settings applied.
        /// </summary>
        private XDocument LoadAndClean(string path)
        {
            var settings = XmlSecuritySettings.CreateSecureReaderSettings();
            using var reader = XmlReader.Create(path, settings);
            var doc = XDocument.Load(reader);
            CleanUpDocument(doc);
            return doc;
        }

        /// <summary>
        /// Loads an XML document from a stream with security settings applied.
        /// </summary>
        private XDocument LoadAndClean(Stream stream)
        {
            var settings = XmlSecuritySettings.CreateSecureReaderSettings();
            using var reader = XmlReader.Create(stream, settings);
            var doc = XDocument.Load(reader);
            CleanUpDocument(doc);
            return doc;
        }

        /// <summary>
        /// Parses an XML document from a string with security settings applied.
        /// </summary>
        private XDocument ParseAndClean(string content)
        {
            var settings = XmlSecuritySettings.CreateSecureReaderSettings();
            using var reader = XmlReader.Create(new StringReader(content), settings);
            var doc = XDocument.Load(reader);
            CleanUpDocument(doc);
            return doc;
        }

        private void CleanUpDocument(XDocument doc)
        {
            // Remove comments and processing instructions
            doc.DescendantNodes()
               .Where(n => n.NodeType == System.Xml.XmlNodeType.Comment || n.NodeType == System.Xml.XmlNodeType.ProcessingInstruction)
               .Remove();
        }

        /// <summary>
        /// Generate HTML report from a diff.
        /// </summary>
        public string GenerateHtml(DiffMatch diff)
        {
            var formatter = new HtmlSideBySideFormatter();
            return formatter.GenerateHtml(diff);
        }

        /// <summary>
        /// Generate HTML report, optionally embedding JSON diff data.
        /// </summary>
        public string GenerateHtml(DiffMatch diff, bool embedJson)
        {
            if (!embedJson) return GenerateHtml(diff);

            var jsonFormatter = new JsonDiffFormatter(_config);
            string json = jsonFormatter.GenerateJson(diff);
            var formatter = new HtmlSideBySideFormatter();
            return formatter.GenerateHtml(diff, json);
        }

        /// <summary>
        /// Generate HTML report with optional embedded JSON and validation metadata.
        /// </summary>
        public string GenerateHtml(DiffMatch diff, bool embedJson, XmlValidationResult? validationResult)
        {
            if (!embedJson) return GenerateHtml(diff);

            var jsonFormatter = new JsonDiffFormatter(_config);
            string json = validationResult == null
                ? jsonFormatter.GenerateJson(diff)
                : jsonFormatter.GenerateJson(diff, validationResult);

            var formatter = new HtmlSideBySideFormatter();
            return formatter.GenerateHtml(diff, json);
        }

        /// <summary>
        /// Generate JSON diff.
        /// </summary>
        public string GenerateJson(DiffMatch diff)
        {
            var formatter = new JsonDiffFormatter(_config);
            return formatter.GenerateJson(diff);
        }

        /// <summary>
        /// Generate JSON diff including validation metadata.
        /// </summary>
        public string GenerateJson(DiffMatch diff, XmlValidationResult validationResult)
        {
            var formatter = new JsonDiffFormatter(_config);
            return formatter.GenerateJson(diff, validationResult);
        }

        /// <summary>
        /// Generate JSON for validation results only.
        /// </summary>
        public string GenerateValidationJson(XmlValidationResult validationResult)
        {
            var formatter = new JsonDiffFormatter(_config);
            return formatter.GenerateValidationJson(validationResult);
        }

        /// <summary>
        /// Compute a summary of diff node counts.
        /// </summary>
        public DiffSummary GetSummary(DiffMatch diff)
        {
            return DiffSummaryCalculator.Compute(diff);
        }
    }
}
