using System;

namespace XmlComparer.Core
{
    /// <summary>
    /// Extension methods providing convenient string-based comparison operations.
    /// </summary>
    /// <remarks>
    /// <para>This class extends <see cref="string"/> with methods for XML comparison,
    /// enabling a more natural syntax when comparing XML content stored as strings.</para>
    /// <para>These extension methods wrap the static methods in <see cref="XmlComparer"/>
    /// with a more fluent, "string to string" syntax using the <c>CompareXmlTo</c> naming convention.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string oldXml = @"&lt;root&gt;&lt;item id=""1""/&gt;&lt;/root&gt;";
    /// string newXml = @"&lt;root&gt;&lt;item id=""1""/&gt;&lt;item id=""2""/&gt;&lt;/root&gt;";
    ///
    /// // Using extension method - natural "to" syntax
    /// var diff = oldXml.CompareXmlTo(newXml);
    ///
    /// // Generate reports
    /// var result = oldXml.CompareXmlToWithReport(newXml,
    ///     options => options.WithKeyAttributes("id").IncludeHtml());
    /// </code>
    /// </example>
    /// <seealso cref="XmlComparer"/>
    public static class XmlComparerExtensions
    {
        /// <summary>
        /// Compares this XML content string with another XML content string.
        /// </summary>
        /// <param name="originalXmlContent">The original XML content.</param>
        /// <param name="newXmlContent">The new XML content to compare against.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>A <see cref="DiffMatch"/> representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentException">Thrown when content is null or empty.</exception>
        /// <example>
        /// <code>
        /// string oldXml = "&lt;root&gt;&lt;item/&gt;&lt;/root&gt;";
        /// string newXml = "&lt;root&gt;&lt;item/&gt;&lt;item/&gt;&lt;/root&gt;";
        ///
        /// var diff = oldXml.CompareXmlTo(newXml);
        /// Console.WriteLine($"Root type: {diff.Type}");
        /// </code>
        /// </example>
        public static DiffMatch CompareXmlTo(this string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null)
        {
            return XmlComparer.CompareContent(originalXmlContent, newXmlContent, configure);
        }

        /// <summary>
        /// Compares this XML content string with another and generates HTML/JSON reports.
        /// </summary>
        /// <param name="originalXmlContent">The original XML content.</param>
        /// <param name="newXmlContent">The new XML content to compare against.</param>
        /// <param name="configure">Optional action to configure comparison options and output formats.</param>
        /// <returns>An <see cref="XmlComparisonResult"/> containing the diff and generated reports.</returns>
        /// <exception cref="ArgumentException">Thrown when content is null or empty.</exception>
        /// <example>
        /// <code>
        /// string oldXml = "&lt;root&gt;&lt;item/&gt;&lt;/root&gt;";
        /// string newXml = "&lt;root&gt;&lt;item/&gt;&lt;item/&gt;&lt;/root&gt;";
        ///
        /// var result = oldXml.CompareXmlToWithReport(newXml,
        ///     options => options.IncludeHtml().IncludeJson());
        ///
        /// Console.WriteLine(result.Html);
        /// </code>
        /// </example>
        public static XmlComparisonResult CompareXmlToWithReport(this string originalXmlContent, string newXmlContent, Action<XmlComparisonOptions>? configure = null)
        {
            return XmlComparer.CompareContentWithReport(originalXmlContent, newXmlContent, configure);
        }

        /// <summary>
        /// Compares XML at this file path with XML at another file path.
        /// </summary>
        /// <param name="originalXmlPath">The path to the original XML file.</param>
        /// <param name="newXmlPath">The path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options.</param>
        /// <returns>A <see cref="DiffMatch"/> representing the root of the diff tree.</returns>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences or are null/empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
        /// <example>
        /// <code>
        /// string oldFile = @"C:\Data\old.xml";
        /// string newFile = @"C:\Data\new.xml";
        ///
        /// var diff = oldFile.CompareXmlFileTo(newFile,
        ///     options => options.WithKeyAttributes("id"));
        /// </code>
        /// </example>
        public static DiffMatch CompareXmlFileTo(this string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null)
        {
            return XmlComparer.CompareFiles(originalXmlPath, newXmlPath, configure);
        }

        /// <summary>
        /// Compares XML at this file path with XML at another file path and generates HTML/JSON reports.
        /// </summary>
        /// <param name="originalXmlPath">The path to the original XML file.</param>
        /// <param name="newXmlPath">The path to the new XML file.</param>
        /// <param name="configure">Optional action to configure comparison options and output formats.</param>
        /// <returns>An <see cref="XmlComparisonResult"/> containing the diff and generated reports.</returns>
        /// <exception cref="ArgumentException">Thrown when paths contain traversal sequences or are null/empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
        /// <example>
        /// <code>
        /// string oldFile = @"C:\Data\old.xml";
        /// string newFile = @"C:\Data\new.xml";
        ///
        /// var result = oldFile.CompareXmlFileToWithReport(newFile,
        ///     options => options.IncludeHtml().WithKeyAttributes("id"));
        ///
        /// File.WriteAllText(@"C:\Data\diff.html", result.Html!);
        /// </code>
        /// </example>
        public static XmlComparisonResult CompareXmlFileToWithReport(this string originalXmlPath, string newXmlPath, Action<XmlComparisonOptions>? configure = null)
        {
            return XmlComparer.CompareFilesWithReport(originalXmlPath, newXmlPath, configure);
        }
    }
}
