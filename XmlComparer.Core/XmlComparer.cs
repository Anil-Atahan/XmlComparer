using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides static methods for comparing XML documents and generating diff reports.
    /// This is the recommended entry point for most scenarios.
    /// </summary>
    /// <remarks>
    /// <para>This class offers multiple comparison methods supporting different input sources:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="CompareFiles(string, string, Action{XmlComparisonOptions}?)"/> - Compare XML files on disk</description></item>
    ///   <item><description><see cref="CompareContent(string, string, Action{XmlComparisonOptions}?)"/> - Compare XML strings</description></item>
    ///   <item><description><see cref="CompareStreams(Stream, Stream, Action{XmlComparisonOptions}?)"/> - Compare XML streams</description></item>
    /// </list>
    /// <para>Each method has a corresponding "WithReport" variant that generates HTML/JSON output,
    /// and an "Async" variant for asynchronous execution.</para>
    /// </remarks>
    /// <example>
    /// This example shows how to compare two XML files and generate an HTML report:
    /// <code>
    /// var result = XmlComparer.CompareFilesWithReport(
    ///     @"C:\Data\old.xml",
    ///     @"C:\Data\new.xml",
    ///     options => options
    ///         .WithKeyAttributes("id", "code")
    ///         .IncludeHtml()
    ///         .IncludeJson());
    ///
    /// File.WriteAllText(@"C:\Data\diff.html", result.Html!);
    /// </code>
    /// </example>
    public static class XmlComparer
    {
        /// <summary>
        /// Compares two XML files and returns the diff result without generating reports.
        /// </summary>
        /// <param name="originalXmlPath">The absolute or relative path to the original XML file.</param>
        /// <param name="newXmlPath">The absolute or relative path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options. See <see cref="XmlComparisonOptions"/> for available settings.</param>
        /// <returns>A <see cref="DiffMatch"/> representing the root of the diff tree. Traverse <see cref="DiffMatch.Children"/> recursively to examine all changes.</returns>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences (..) or are null/empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when file size exceeds maximum allowed (100MB).</exception>
        /// <exception cref="XmlException">Thrown when either file contains malformed XML.</exception>
        /// <example>
        /// Compare two files with default settings:
        /// <code>
        /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
        /// if (diff.Type != DiffType.Unchanged)
        /// {
        ///     Console.WriteLine($"Files differ: {diff.Type}");
        /// }
        /// </code>
        /// </example>
        public static DiffMatch CompareFiles(string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null)
        {
            return CompareFilesWithReport(originalXmlPath, newXmlPath, configure).Diff;
        }

        /// <summary>
        /// Compares two XML streams and returns the diff result without generating reports.
        /// </summary>
        /// <param name="originalStream">Stream containing the original XML.</param>
        /// <param name="newStream">Stream containing the new XML.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>A <see cref="DiffMatch"/> representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either stream is null.</exception>
        public static DiffMatch CompareStreams(Stream originalStream, Stream newStream, Action<XmlComparisonOptions>? configure = null)
        {
            return CompareStreamsWithReport(originalStream, newStream, configure).Diff;
        }

        /// <summary>
        /// Compares two XML content strings and returns the diff result without generating reports.
        /// </summary>
        /// <param name="originalXmlContent">The original XML content as a string.</param>
        /// <param name="newXmlContent">The new XML content as a string.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>A <see cref="DiffMatch"/> representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentException">Thrown when either content is null or empty.</exception>
        public static DiffMatch CompareContent(string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null)
        {
            return CompareContentWithReport(originalXmlContent, newXmlContent, configure).Diff;
        }

        /// <summary>
        /// Compares two XML files asynchronously and returns the diff result without generating reports.
        /// </summary>
        /// <param name="originalXmlPath">The absolute or relative path to the original XML file.</param>
        /// <param name="newXmlPath">The absolute or relative path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with <see cref="DiffMatch"/> result.</returns>
        public static Task<DiffMatch> CompareFilesAsync(string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null, CancellationToken cancellationToken = default)
        {
            return CompareFilesWithReportAsync(originalXmlPath, newXmlPath, configure, cancellationToken)
                .ContinueWith(t => t.Result.Diff, cancellationToken);
        }

        /// <summary>
        /// Compares two XML content strings asynchronously and returns the diff result without generating reports.
        /// </summary>
        /// <param name="originalXmlContent">The original XML content as a string.</param>
        /// <param name="newXmlContent">The new XML content as a string.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with <see cref="DiffMatch"/> result.</returns>
        public static Task<DiffMatch> CompareContentAsync(string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null, CancellationToken cancellationToken = default)
        {
            return CompareContentWithReportAsync(originalXmlContent, newXmlContent, configure, cancellationToken)
                .ContinueWith(t => t.Result.Diff, cancellationToken);
        }

        /// <summary>
        /// Compares two XML files and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalXmlPath">The absolute or relative path to the original XML file.</param>
        /// <param name="newXmlPath">The absolute or relative path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options and output formats. Use <see cref="XmlComparisonOptions.IncludeHtml"/> and <see cref="XmlComparisonOptions.IncludeJson"/> to generate reports.</param>
        /// <returns>An <see cref="XmlComparisonResult"/> containing the diff, optional validation results, and generated reports.</returns>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences or are null/empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when either input file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when file size exceeds maximum or XSD validation fails.</exception>
        /// <exception cref="XmlException">Thrown when either file contains malformed XML.</exception>
        /// <example>
        /// Generate both HTML and JSON reports:
        /// <code>
        /// var result = XmlComparer.CompareFilesWithReport(
        ///     "old.xml",
        ///     "new.xml",
        ///     options => options.IncludeHtml().IncludeJson());
        ///
        /// await File.WriteAllTextAsync("diff.html", result.Html!);
        /// await File.WriteAllTextAsync("diff.json", result.Json!);
        /// </code>
        /// </example>
        public static XmlComparisonResult CompareFilesWithReport(string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null)
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
        /// <returns>An <see cref="XmlComparisonResult"/> containing the diff and generated reports.</returns>
        public static XmlComparisonResult CompareStreamsWithReport(Stream originalStream, Stream newStream, Action<XmlComparisonOptions>? configure = null)
        {
            var options = BuildOptions(configure);
            var service = new XmlComparerService(options.Config, options.MatchingStrategy);

            var diff = service.Compare(originalStream, newStream);
            return BuildResult(service, options, diff, validation: null);
        }

        /// <summary>
        /// Compares two XML content strings and generates HTML and/or JSON reports.
        /// </summary>
        /// <param name="originalXmlContent">The original XML content as a string.</param>
        /// <param name="newXmlContent">The new XML content as a string.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>An <see cref="XmlComparisonResult"/> containing the diff, optional validation results, and generated reports.</returns>
        public static XmlComparisonResult CompareContentWithReport(string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null)
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
        /// <param name="originalXmlPath">The absolute or relative path to the original XML file.</param>
        /// <param name="newXmlPath">The absolute or relative path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with <see cref="XmlComparisonResult"/> result.</returns>
        public static async Task<XmlComparisonResult> CompareFilesWithReportAsync(string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null, CancellationToken cancellationToken = default)
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
        /// <param name="originalXmlContent">The original XML content as a string.</param>
        /// <param name="newXmlContent">The new XML content as a string.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the async operation with <see cref="XmlComparisonResult"/> result.</returns>
        public static async Task<XmlComparisonResult> CompareContentWithReportAsync(string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null, CancellationToken cancellationToken = default)
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
        /// Builds options from the configuration action.
        /// </summary>
        private static XmlComparisonOptions BuildOptions(Action<XmlComparisonOptions>? configure)
        {
            var options = new XmlComparisonOptions();
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
    }
}
