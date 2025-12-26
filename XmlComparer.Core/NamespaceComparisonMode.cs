namespace XmlComparer.Core
{
    /// <summary>
    /// Defines how XML namespaces are handled during comparison.
    /// </summary>
    /// <remarks>
    /// <para>XML namespaces can be a significant source of perceived differences in XML documents.
    /// This enum controls how namespace differences affect element matching and diff detection.</para>
    /// <para><b>Comparison Modes:</b></para>
    /// <list type="bullet">
    ///   <item><description><see cref="Ignore"/> - Treat elements with different namespaces as equal if local names match</description></item>
    ///   <item><description><see cref="Strict"/> - Require exact namespace match (both URI and prefix)</description></item>
    ///   <item><description><see cref="UriSensitive"/> - Track namespace URI changes but ignore prefix differences</description></item>
    ///   <item><description><see cref="PrefixPreserve"/> - Track prefix changes but ignore namespace URI differences</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Ignore namespace differences
    /// options.WithNamespaceComparison(NamespaceComparisonMode.Ignore);
    ///
    /// // Require exact namespace match
    /// options.WithNamespaceComparison(NamespaceComparisonMode.Strict);
    ///
    /// // Detect namespace URI changes (but not prefix changes)
    /// options.WithNamespaceComparison(NamespaceComparisonMode.UriSensitive);
    /// </code>
    /// </example>
    public enum NamespaceComparisonMode
    {
        /// <summary>
        /// Treat elements with different namespaces as equal if their local names match.
        /// </summary>
        /// <remarks>
        /// This is the default behavior for backward compatibility.
        /// Namespace prefixes and URIs are completely ignored during comparison.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;ns1:foo/&gt; and &lt;ns2:foo/&gt; will be considered equal.
        /// </code>
        /// </example>
        Ignore,

        /// <summary>
        /// Require exact namespace match (both URI and prefix) for elements to be considered equal.
        /// </summary>
        /// <remarks>
        /// Elements with different namespace URIs or different namespace prefixes will be
        /// treated as different elements, potentially resulting in Added/Deleted diffs.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;ns1:foo xmlns:ns1="http://example.com"/&gt; and
        /// &lt;ns2:foo xmlns:ns2="http://example.com"/&gt;
        /// will NOT be considered equal (prefix differs).
        /// </code>
        /// </example>
        Strict,

        /// <summary>
        /// Track namespace URI changes but ignore prefix differences.
        /// </summary>
        /// <remarks>
        /// Elements with the same local name and namespace URI are considered equal regardless
        /// of the namespace prefix used. However, if the namespace URI changes, the element
        /// will be flagged with <see cref="DiffType.NamespaceChanged"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;ns1:foo xmlns:ns1="http://example.com"/&gt; and
        /// &lt;ns2:foo xmlns:ns2="http://example.com"/&gt;
        /// will be considered equal (same URI, different prefix).
        ///
        /// &lt;ns1:foo xmlns:ns1="http://example.com/v1"/&gt; and
        /// &lt;ns1:foo xmlns:ns1="http://example.com/v2"/&gt;
        /// will have NamespaceChanged type (URI differs).
        /// </code>
        /// </example>
        UriSensitive,

        /// <summary>
        /// Track prefix changes but ignore namespace URI differences.
        /// </summary>
        /// <remarks>
        /// Elements are considered equal if they have the same local name and namespace prefix,
        /// regardless of the actual namespace URI. If the prefix changes, the element will be
        /// flagged with <see cref="DiffType.NamespaceChanged"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;ns1:foo xmlns:ns1="http://example.com/v1"/&gt; and
        /// &lt;ns1:foo xmlns:ns1="http://example.com/v2"/&gt;
        /// will be considered equal (same prefix, different URI).
        ///
        /// &lt;ns1:foo xmlns:ns1="http://example.com"/&gt; and
        /// &lt;ns2:foo xmlns:ns2="http://example.com"/&gt;
        /// will have NamespaceChanged type (prefix differs).
        /// </code>
        /// </example>
        PrefixPreserve
    }
}
