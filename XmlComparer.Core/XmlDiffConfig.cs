using System.Collections.Generic;

namespace XmlComparer.Core
{
    /// <summary>
    /// Registry for custom value normalizers that can be applied during XML comparison.
    /// </summary>
    /// <remarks>
    /// This static registry allows you to register and retrieve custom value normalizers
    /// by identifier for serialization and dependency injection scenarios.
    /// </remarks>
    public static class ValueNormalizerRegistry
    {
        private static readonly Dictionary<string, IValueNormalizer> _normalizers = new();

        /// <summary>
        /// Registers a value normalizer with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier for this normalizer.</param>
        /// <param name="normalizer">The normalizer instance.</param>
        /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when normalizer is null.</exception>
        public static void Register(string id, IValueNormalizer normalizer)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Normalizer id is required.", nameof(id));
            _normalizers[id] = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        }

        /// <summary>
        /// Resolves a value normalizer instance from its identifier.
        /// </summary>
        /// <param name="id">The identifier of the normalizer to resolve.</param>
        /// <returns>The normalizer instance, or null if not found.</returns>
        public static IValueNormalizer? Resolve(string id)
        {
            return _normalizers.TryGetValue(id, out var normalizer) ? normalizer : null;
        }

        /// <summary>
        /// Clears all registered normalizers.
        /// </summary>
        public static void Clear()
        {
            _normalizers.Clear();
        }
    }

    /// <summary>
    /// Configuration settings for XML diff generation.
    /// </summary>
    /// <remarks>
    /// <para>This class encapsulates all settings that control how XML elements are compared.
    /// It is used internally by <see cref="XmlComparerService"/> and <see cref="XmlComparerClient"/>,
    /// and is typically configured through the fluent <see cref="XmlComparisonOptions"/> API.</para>
    /// <para><b>Key Configuration Concepts:</b></para>
    /// <list type="bullet">
    ///   <item><description><see cref="KeyAttributeNames"/> - Enables move detection by matching elements via their key attributes</description></item>
    ///   <item><description><see cref="ExcludedNodeNames"/> - Elements to ignore during comparison</description></item>
    ///   <item><description><see cref="ExcludedAttributeNames"/> - Attributes to ignore during comparison</description></item>
    ///   <item><description><see cref="NormalizeWhitespace"/>, <see cref="TrimValues"/>, <see cref="NormalizeNewlines"/> - Value normalization options</description></item>
    ///   <item><description><see cref="IgnoreValues"/> - Skip text content comparison entirely</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Direct configuration (typically done via <see cref="XmlComparisonOptions"/> instead):
    /// <code>
    /// var config = new XmlDiffConfig
    /// {
    ///     KeyAttributeNames = { "id", "code" },
    ///     ExcludedAttributeNames = { "timestamp" },
    ///     NormalizeWhitespace = true,
    ///     TrimValues = true
    /// };
    ///
    /// var service = new XmlComparerService(config);
    /// var diff = service.Compare("old.xml", "new.xml");
    /// </code>
    /// </example>
    /// <seealso cref="XmlComparisonOptions"/>
    public class XmlDiffConfig
    {
        /// <summary>
        /// Gets or sets the set of element names to exclude from comparison.
        /// </summary>
        /// <remarks>
        /// Elements with names in this set are ignored during comparison.
        /// When <see cref="ExcludeSubtree"/> is true, the entire subtree under excluded nodes is also ignored.
        /// </remarks>
        /// <example>
        /// <code>
        /// config.ExcludedNodeNames.Add("Comment");
        /// config.ExcludedNodeNames.Add("Metadata");
        /// </code>
        /// </example>
        public HashSet<string> ExcludedNodeNames { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the set of attribute names to exclude from comparison.
        /// </summary>
        /// <remarks>
        /// Attributes with names in this set are ignored during comparison.
        /// This is useful for excluding volatile attributes like timestamps or version numbers.
        /// </remarks>
        /// <example>
        /// <code>
        /// config.ExcludedAttributeNames.Add("lastModified");
        /// config.ExcludedAttributeNames.Add("version");
        /// </code>
        /// </example>
        public HashSet<string> ExcludedAttributeNames { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the set of attribute names to use as keys for element matching.
        /// </summary>
        /// <remarks>
        /// <para>When key attributes are specified, elements are matched based on their key attribute values
        /// rather than their position. This enables the detection of moved elements.</para>
        /// <para>Multiple key attributes can be specified; all must match for elements to be considered equal.</para>
        /// <para><b>Without key attributes:</b> Elements are matched by position (first element to first element).</para>
        /// <para><b>With key attributes:</b> Elements are matched by their key values (id="abc" to id="abc").</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Enable move detection using 'id' attribute
        /// config.KeyAttributeNames.Add("id");
        ///
        /// // Use multiple attributes for composite keys
        /// config.KeyAttributeNames.Add("id");
        /// config.KeyAttributeNames.Add("region");
        /// </code>
        /// </example>
        public HashSet<string> KeyAttributeNames { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets whether to exclude the entire subtree of excluded nodes.
        /// </summary>
        /// <remarks>
        /// When true, all descendants of excluded nodes are also excluded from comparison.
        /// When false, only the excluded nodes themselves are ignored, but their children are still compared.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Exclude entire comment subtrees
        /// config.ExcludeSubtree = true;
        /// config.ExcludedNodeNames.Add("Comment");
        /// </code>
        /// </example>
        public bool ExcludeSubtree { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to normalize whitespace in text values during comparison.
        /// </summary>
        /// <remarks>
        /// When true, consecutive whitespace characters (spaces, tabs, newlines) are collapsed into a single space.
        /// This is useful for comparing XML where formatting may differ but content is the same.
        /// </remarks>
        /// <example>
        /// <code>
        /// // "Hello    world" will equal "Hello world"
        /// config.NormalizeWhitespace = true;
        /// </code>
        /// </example>
        public bool NormalizeWhitespace { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to trim leading and trailing whitespace from text values.
        /// </summary>
        /// <remarks>
        /// When true, leading and trailing whitespace is removed from text values before comparison.
        /// </remarks>
        /// <example>
        /// <code>
        /// // "  Hello  " will equal "Hello"
        /// config.TrimValues = true;
        /// </code>
        /// </example>
        public bool TrimValues { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to normalize newline characters in text values.
        /// </summary>
        /// <remarks>
        /// When true, all newline styles (CRLF, LF, CR) are normalized to a consistent format (LF).
        /// This is useful for comparing XML files created on different operating systems.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Normalizes Windows (CRLF) and Unix (LF) line endings
        /// config.NormalizeNewlines = true;
        /// </code>
        /// </example>
        public bool NormalizeNewlines { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to ignore text content values during comparison.
        /// </summary>
        /// <remarks>
        /// When true, only element structure and attributes are compared; text content is ignored.
        /// This is useful when only the document structure matters, not the actual data values.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Only compare structure, ignore text values
        /// config.IgnoreValues = true;
        /// </code>
        /// </example>
        public bool IgnoreValues { get; set; } = false;

        /// <summary>
        /// Gets or sets the namespace comparison mode for XML documents.
        /// </summary>
        /// <remarks>
        /// <para>Controls how namespace differences affect element matching and diff detection.</para>
        /// <para>The default value is <see cref="NamespaceComparisonMode.Ignore"/>, which treats
        /// elements with different namespaces as equal if their local names match (backward compatible).</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Require exact namespace match
        /// config.NamespaceComparisonMode = NamespaceComparisonMode.Strict;
        ///
        /// // Track namespace URI changes but ignore prefix differences
        /// config.NamespaceComparisonMode = NamespaceComparisonMode.UriSensitive;
        /// </code>
        /// </example>
        public NamespaceComparisonMode NamespaceComparisonMode { get; set; } = NamespaceComparisonMode.Ignore;

        /// <summary>
        /// Gets or sets whether to treat namespace prefix changes as modifications.
        /// </summary>
        /// <remarks>
        /// <para>When true and <see cref="NamespaceComparisonMode"/> is set to
        /// <see cref="NamespaceComparisonMode.UriSensitive"/>, namespace prefix changes
        /// (without URI changes) will be flagged as <see cref="DiffType.Modified"/> instead of ignored.</para>
        /// <para>This is useful when documenting namespace changes in XML schemas where
        /// the prefix itself matters for downstream processing.</para>
        /// </remarks>
        public bool TrackPrefixChanges { get; set; } = false;

        /// <summary>
        /// Gets or sets the settings for preserving and comparing non-element XML nodes.
        /// </summary>
        /// <remarks>
        /// <para>By default, CDATA sections, comments, and processing instructions are stripped during
        /// document loading. Set this property to enable preservation of these nodes.</para>
        /// <para>The default value is null, which means no non-element nodes are preserved.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Preserve comments and CDATA sections
        /// config.NodePreservation = new XmlNodePreservationSettings
        /// {
        ///     Mode = XmlNodePreservationMode.CommentsOnly
        /// };
        /// </code>
        /// </example>
        public XmlNodePreservationSettings? NodePreservation { get; set; }

        /// <summary>
        /// Gets the collection of custom value normalizers to apply during comparison.
        /// </summary>
        /// <remarks>
        /// <para>Custom normalizers are applied after the built-in normalizers (whitespace, newlines, trimming).
        /// Each normalizer in the collection is applied in sequence to transform values before comparison.</para>
        /// <para>Use <see cref="XmlComparisonOptions.UseValueNormalizer(IValueNormalizer)"/> to add
        /// normalizers via the fluent API.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// config.ValueNormalizers.Add(new DateTimeNormalizer());
        /// config.ValueNormalizers.Add(new NumericNormalizer(0.001));
        /// </code>
        /// </example>
        public List<IValueNormalizer> ValueNormalizers { get; } = new List<IValueNormalizer>();
    }
}
