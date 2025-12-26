using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides helper methods for XML namespace comparison and manipulation.
    /// </summary>
    /// <remarks>
    /// <para>This class contains utility methods for comparing XML namespaces according to
    /// different comparison modes defined in <see cref="NamespaceComparisonMode"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// bool equal = XmlNamespaceHelper.NamespacesEqual(
    ///     element1.Name,
    ///     element2.Name,
    ///     NamespaceComparisonMode.UriSensitive);
    /// </code>
    /// </example>
    public static class XmlNamespaceHelper
    {
        /// <summary>
        /// Determines whether two XML names are equal according to the specified namespace comparison mode.
        /// </summary>
        /// <param name="name1">The first XML name.</param>
        /// <param name="name2">The second XML name.</param>
        /// <param name="mode">The namespace comparison mode.</param>
        /// <returns>True if the names are equal according to the specified mode; false otherwise.</returns>
        /// <example>
        /// <code>
        /// var name1 = XElement.Parse("&lt;ns1:foo xmlns:ns1=\"http://example.com\"/&gt;").Name;
        /// var name2 = XElement.Parse("&lt;ns2:foo xmlns:ns2=\"http://example.com\"/&gt;").Name;
        ///
        /// // Ignore mode: equal (local names match)
        /// XmlNamespaceHelper.NamespacesEqual(name1, name2, NamespaceComparisonMode.Ignore); // true
        ///
        /// // Strict mode: not equal (prefixes differ)
        /// XmlNamespaceHelper.NamespacesEqual(name1, name2, NamespaceComparisonMode.Strict); // false
        ///
        /// // UriSensitive mode: equal (URIs match)
        /// XmlNamespaceHelper.NamespacesEqual(name1, name2, NamespaceComparisonMode.UriSensitive); // true
        /// </code>
        /// </example>
        public static bool NamespacesEqual(XName name1, XName name2, NamespaceComparisonMode mode)
        {
            // Local names must always match
            if (name1.LocalName != name2.LocalName)
                return false;

            switch (mode)
            {
                case NamespaceComparisonMode.Ignore:
                    // Only local names matter
                    return true;

                case NamespaceComparisonMode.Strict:
                    // Both namespace URI and prefix must match exactly
                    return name1.NamespaceName == name2.NamespaceName;

                case NamespaceComparisonMode.UriSensitive:
                    // Namespace URI must match, prefix doesn't matter
                    return GetNamespaceUri(name1) == GetNamespaceUri(name2);

                case NamespaceComparisonMode.PrefixPreserve:
                    // Prefix must match, URI doesn't matter
                    return GetNamespacePrefix(name1) == GetNamespacePrefix(name2);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the namespace has changed between two elements.
        /// </summary>
        /// <param name="originalElement">The original element.</param>
        /// <param name="newElement">The new element.</param>
        /// <param name="mode">The namespace comparison mode.</param>
        /// <returns>True if the namespace has changed according to the specified mode.</returns>
        /// <example>
        /// <code>
        /// var elem1 = XElement.Parse("&lt;ns1:foo xmlns:ns1=\"http://example.com/v1\"/&gt;");
        /// var elem2 = XElement.Parse("&lt;ns1:foo xmlns:ns1=\"http://example.com/v2\"/&gt;");
        ///
        /// bool changed = XmlNamespaceHelper.HasNamespaceChanged(
        ///     elem1, elem2, NamespaceComparisonMode.UriSensitive);
        /// // changed = true (URI differs)
        /// </code>
        /// </example>
        public static bool HasNamespaceChanged(XElement? originalElement, XElement? newElement, NamespaceComparisonMode mode)
        {
            if (originalElement == null || newElement == null)
                return false;

            // If ignoring namespaces, no change detected
            if (mode == NamespaceComparisonMode.Ignore)
                return false;

            return !NamespacesEqual(originalElement.Name, newElement.Name, mode);
        }

        /// <summary>
        /// Gets the namespace prefix for an XML name.
        /// </summary>
        /// <param name="name">The XML name.</param>
        /// <returns>The namespace prefix, or an empty string if none exists.</returns>
        /// <example>
        /// <code>
        /// var name = XNamespace.Get("http://example.com") + "foo";
        /// string prefix = XmlNamespaceHelper.GetNamespacePrefix(name);
        /// // prefix = "" (no prefix for expanded names)
        ///
        /// var elem = XElement.Parse("&lt;ns:foo xmlns:ns=\"http://example.com\"/&gt;");
        /// prefix = XmlNamespaceHelper.GetNamespacePrefix(elem.Name);
        /// // prefix = "ns"
        /// </code>
        /// </example>
        public static string GetNamespacePrefix(XName name)
        {
            // XName doesn't expose prefix directly - need to get it from the string representation
            string expandedName = name.ToString();
            int colonIndex = expandedName.IndexOf(':');
            return colonIndex > 0 ? expandedName.Substring(0, colonIndex) : string.Empty;
        }

        /// <summary>
        /// Gets the namespace URI for an XML name.
        /// </summary>
        /// <param name="name">The XML name.</param>
        /// <returns>The namespace URI, or empty if no namespace.</returns>
        /// <example>
        /// <code>
        /// var name = XNamespace.Get("http://example.com") + "foo";
        /// string uri = XmlNamespaceHelper.GetNamespaceUri(name);
        /// // uri = "http://example.com"
        /// </code>
        /// </example>
        public static string GetNamespaceUri(XName name)
        {
            return name.NamespaceName ?? string.Empty;
        }

        /// <summary>
        /// Builds a dictionary mapping namespace prefixes to their URIs for a given element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>A dictionary of namespace prefixes to URIs.</returns>
        /// <remarks>
        /// This method scans the element and its ancestors for namespace declarations.
        /// </remarks>
        /// <example>
        /// <code>
        /// var elem = XElement.Parse("&lt;root xmlns:ns1=\"http://example.com/v1\"&gt;&lt;/root&gt;");
        /// var namespaces = XmlNamespaceHelper.BuildNamespaceMap(elem);
        /// // namespaces["ns1"] = "http://example.com/v1"
        /// // namespaces["xml"] = "http://www.w3.org/XML/1998/namespace"
        /// </code>
        /// </example>
        public static Dictionary<string, string> BuildNamespaceMap(XElement element)
        {
            var map = new Dictionary<string, string>();

            // Add built-in XML namespace
            map["xml"] = "http://www.w3.org/XML/1998/namespace";

            // Walk up the tree to collect all namespace declarations
            XElement? current = element;
            while (current != null)
            {
                foreach (var attr in current.Attributes())
                {
                    if (attr.Name.Namespace == XNamespace.Xmlns)
                    {
                        string prefix = attr.Name.LocalName;
                        string uri = attr.Value;

                        // Only add if not already present (child scopes override parent)
                        if (!map.ContainsKey(prefix))
                        {
                            map[prefix] = uri;
                        }
                    }
                }

                current = current.Parent;
            }

            return map;
        }

        /// <summary>
        /// Gets a human-readable description of the namespace difference between two elements.
        /// </summary>
        /// <param name="originalElement">The original element.</param>
        /// <param name="newElement">The new element.</param>
        /// <param name="mode">The namespace comparison mode.</param>
        /// <returns>A description of the namespace change, or null if no change.</returns>
        /// <example>
        /// <code>
        /// var elem1 = XElement.Parse("&lt;ns1:foo xmlns:ns1=\"http://example.com/v1\"/&gt;");
        /// var elem2 = XElement.Parse("&lt;ns2:foo xmlns:ns2=\"http://example.com/v2\"/&gt;");
        ///
        /// string description = XmlNamespaceHelper.GetNamespaceChangeDescription(
        ///     elem1, elem2, NamespaceComparisonMode.UriSensitive);
        /// // description = "Namespace URI changed from 'http://example.com/v1' to 'http://example.com/v2'"
        /// </code>
        /// </example>
        public static string? GetNamespaceChangeDescription(XElement originalElement, XElement newElement, NamespaceComparisonMode mode)
        {
            if (!HasNamespaceChanged(originalElement, newElement, mode))
                return null;

            string originalPrefix = GetNamespacePrefix(originalElement.Name);
            string newPrefix = GetNamespacePrefix(newElement.Name);
            string originalUri = GetNamespaceUri(originalElement.Name);
            string newUri = GetNamespaceUri(newElement.Name);

            switch (mode)
            {
                case NamespaceComparisonMode.UriSensitive:
                    if (originalUri != newUri)
                        return $"Namespace URI changed from '{originalUri}' to '{newUri}'";
                    break;

                case NamespaceComparisonMode.PrefixPreserve:
                    if (originalPrefix != newPrefix)
                        return $"Namespace prefix changed from '{originalPrefix}' to '{newPrefix}'";
                    break;

                case NamespaceComparisonMode.Strict:
                    return $"Namespace changed from '{originalPrefix}:{originalUri}' to '{newPrefix}:{newUri}'";
            }

            return "Namespace changed";
        }
    }
}
