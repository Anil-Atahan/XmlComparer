using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace XmlComparer.Core
{
    /// <summary>
    /// Dependency injection-friendly client for XML comparison operations.
    /// </summary>
    /// <remarks>
    /// <para>This class provides a reusable instance that maintains its configuration and strategy,
    /// making it ideal for dependency injection scenarios in web applications and services.</para>
    /// <para>Unlike the static <see cref="XmlComparer"/> class, instances of <c>XmlComparerClient</c>
    /// maintain their state and can be registered as singletons or scoped services.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In ConfigureServices (ASP.NET Core)
    /// services.AddSingleton&lt;XmlComparerClient&gt;(new XmlComparerClient(
    ///     new XmlDiffConfig { IgnoreValues = false },
    ///     new DefaultMatchingStrategy()));
    ///
    /// // In controller
    /// public class MyController : Controller
    /// {
    ///     private readonly XmlComparerClient _comparer;
    ///
    ///     public MyController(XmlComparerClient comparer)
    ///     {
    ///         _comparer = comparer;
    ///     }
    ///
    ///     public IActionResult Compare()
    ///     {
    ///         var result = _comparer.CompareContentWithReport(
    ///             xml1, xml2,
    ///             options => options.IncludeHtml());
    ///         return Content(result.Html, "text/html");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="XmlComparer"/>
    /// <seealso cref="XmlComparerService"/>
    public class XmlComparerClient
    {
        private readonly XmlDiffConfig _config;
        private readonly IMatchingStrategy? _strategy;

        /// <summary>
        /// Creates a new XmlComparerClient with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration for XML comparison.</param>
        /// <param name="strategy">Optional custom matching strategy for element comparison.</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        public XmlComparerClient(XmlDiffConfig config, IMatchingStrategy? strategy = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _strategy = strategy;
        }

        /// <summary>
        /// Compares two XML files and returns the diff result.
        /// </summary>
        /// <param name="originalXmlPath">Path to the original XML file.</param>
        /// <param name="newXmlPath">Path to the new XML file.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences.</exception>
        /// <exception cref="FileNotFoundException">Thrown when files don't exist.</exception>
        public DiffMatch CompareFiles(string originalXmlPath, string newXmlPath)
        {
            var service = new XmlComparerService(_config, _strategy);
            return service.Compare(originalXmlPath, newXmlPath);
        }

        /// <summary>
        /// Compares two XML streams and returns the diff result.
        /// </summary>
        /// <param name="originalStream">Stream containing the original XML.</param>
        /// <param name="newStream">Stream containing the new XML.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        public DiffMatch CompareStreams(Stream originalStream, Stream newStream)
        {
            var service = new XmlComparerService(_config, _strategy);
            return service.Compare(originalStream, newStream);
        }

        /// <summary>
        /// Compares two XML content strings and returns the diff result.
        /// </summary>
        /// <param name="originalXmlContent">Original XML content string.</param>
        /// <param name="newXmlContent">New XML content string.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        public DiffMatch CompareContent(string originalXmlContent, string newXmlContent)
        {
            var service = new XmlComparerService(_config, _strategy);
            return service.CompareXml(originalXmlContent, newXmlContent);
        }

        /// <summary>
        /// Compares two XML files asynchronously and returns the diff result.
        /// </summary>
        /// <param name="originalXmlPath">Path to the original XML file.</param>
        /// <param name="newXmlPath">Path to the new XML file.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with DiffMatch result.</returns>
        public Task<DiffMatch> CompareFilesAsync(string originalXmlPath, string newXmlPath, CancellationToken cancellationToken = default)
        {
            var service = new XmlComparerService(_config, _strategy);
            return service.CompareAsync(originalXmlPath, newXmlPath, cancellationToken);
        }

        /// <summary>
        /// Compares two XML content strings asynchronously and returns the diff result.
        /// </summary>
        /// <param name="originalXmlContent">Original XML content string.</param>
        /// <param name="newXmlContent">New XML content string.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with DiffMatch result.</returns>
        public Task<DiffMatch> CompareContentAsync(string originalXmlContent, string newXmlContent, CancellationToken cancellationToken = default)
        {
            var service = new XmlComparerService(_config, _strategy);
            return service.CompareXmlAsync(originalXmlContent, newXmlContent, cancellationToken);
        }

        /// <summary>
        /// Compares two XML files and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalXmlPath">Path to the original XML file.</param>
        /// <param name="newXmlPath">Path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options and output formats.</param>
        /// <returns>An XmlComparisonResult containing the diff, validation results, and generated reports.</returns>
        public XmlComparisonResult CompareFilesWithReport(string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null)
        {
            var options = BuildOptions(configure);
            var service = new XmlComparerService(options.Config, options.MatchingStrategy);

            XmlValidationResult? validation = null;
            if (options.XsdPaths.Count > 0)
            {
                validation = service.ValidateXmlFiles(options.XsdPaths, originalXmlPath, newXmlPath);
            }

            var diff = service.Compare(originalXmlPath, newXmlPath);
            return BuildResult(service, options, diff, validation);
        }

        /// <summary>
        /// Compares two XML streams and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalStream">Stream containing the original XML.</param>
        /// <param name="newStream">Stream containing the new XML.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>An XmlComparisonResult containing the diff and generated reports.</returns>
        public XmlComparisonResult CompareStreamsWithReport(Stream originalStream, Stream newStream, Action<XmlComparisonOptions>? configure = null)
        {
            var options = BuildOptions(configure);
            var service = new XmlComparerService(options.Config, options.MatchingStrategy);

            var diff = service.Compare(originalStream, newStream);
            return BuildResult(service, options, diff, validation: null);
        }

        /// <summary>
        /// Compares two XML content strings and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalXmlContent">Original XML content string.</param>
        /// <param name="newXmlContent">New XML content string.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>An XmlComparisonResult containing the diff, validation results, and generated reports.</returns>
        public XmlComparisonResult CompareContentWithReport(string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null)
        {
            var options = BuildOptions(configure);
            var service = new XmlComparerService(options.Config, options.MatchingStrategy);

            XmlValidationResult? validation = null;
            if (options.XsdPaths.Count > 0)
            {
                validation = service.ValidateXmlContent(options.XsdPaths, originalXmlContent, newXmlContent);
            }

            var diff = service.CompareXml(originalXmlContent, newXmlContent);
            return BuildResult(service, options, diff, validation);
        }

        /// <summary>
        /// Compares two XML files asynchronously and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalXmlPath">Path to the original XML file.</param>
        /// <param name="newXmlPath">Path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with XmlComparisonResult result.</returns>
        public async Task<XmlComparisonResult> CompareFilesWithReportAsync(string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null, CancellationToken cancellationToken = default)
        {
            var options = BuildOptions(configure);
            var service = new XmlComparerService(options.Config, options.MatchingStrategy);

            XmlValidationResult? validation = null;
            if (options.XsdPaths.Count > 0)
            {
                validation = await service.ValidateXmlFilesAsync(options.XsdPaths, originalXmlPath, newXmlPath, cancellationToken).ConfigureAwait(false);
            }

            var diff = await service.CompareAsync(originalXmlPath, newXmlPath, cancellationToken).ConfigureAwait(false);
            return BuildResult(service, options, diff, validation);
        }

        /// <summary>
        /// Compares two XML content strings asynchronously and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalXmlContent">Original XML content string.</param>
        /// <param name="newXmlContent">New XML content string.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with XmlComparisonResult result.</returns>
        public async Task<XmlComparisonResult> CompareContentWithReportAsync(string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null, CancellationToken cancellationToken = default)
        {
            var options = BuildOptions(configure);
            var service = new XmlComparerService(options.Config, options.MatchingStrategy);

            XmlValidationResult? validation = null;
            if (options.XsdPaths.Count > 0)
            {
                validation = await service.ValidateXmlContentAsync(options.XsdPaths, originalXmlContent, newXmlContent, cancellationToken).ConfigureAwait(false);
            }

            var diff = await service.CompareXmlAsync(originalXmlContent, newXmlContent, cancellationToken).ConfigureAwait(false);
            return BuildResult(service, options, diff, validation);
        }

        /// <summary>
        /// Builds options by copying the instance configuration and applying optional modifications.
        /// </summary>
        private XmlComparisonOptions BuildOptions(Action<XmlComparisonOptions>? configure)
        {
            var options = new XmlComparisonOptions();
            CopyConfig(_config, options.Config);
            if (_strategy != null)
            {
                options.UseMatchingStrategy(_strategy);
            }
            configure?.Invoke(options);
            return options;
        }

        /// <summary>
        /// Builds the result object with generated reports.
        /// </summary>
        private static XmlComparisonResult BuildResult(XmlComparerService service, XmlComparisonOptions options, DiffMatch diff, XmlValidationResult? validation)
        {
            string? html = null;
            if (options.GenerateHtml)
            {
                html = service.GenerateHtml(diff, options.EmbedJsonInHtml, validation);
            }

            string? json = null;
            if (options.GenerateJson)
            {
                json = (validation != null && options.IncludeValidationInJson)
                    ? service.GenerateJson(diff, validation)
                    : service.GenerateJson(diff);
            }

            return new XmlComparisonResult(diff, validation, html, json);
        }

        /// <summary>
        /// Copies configuration settings from source to target.
        /// </summary>
        private static void CopyConfig(XmlDiffConfig source, XmlDiffConfig target)
        {
            target.ExcludeSubtree = source.ExcludeSubtree;
            target.IgnoreValues = source.IgnoreValues;

            foreach (var name in source.ExcludedNodeNames)
            {
                target.ExcludedNodeNames.Add(name);
            }

            foreach (var name in source.ExcludedAttributeNames)
            {
                target.ExcludedAttributeNames.Add(name);
            }

            foreach (var name in source.KeyAttributeNames)
            {
                target.KeyAttributeNames.Add(name);
            }
        }
    }
}
