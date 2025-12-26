using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Default implementation of <see cref="IMatchingStrategy"/> that scores element similarity
    /// based on name, key attributes, other attributes, and text content.
    /// </summary>
    /// <remarks>
    /// <para>Scoring algorithm:</para>
    /// <list type="bullet">
    ///   <item><description>Base score of 1.0 if element names match</description></item>
    ///   <item><description>+10.0 for each matching key attribute</description></item>
    ///   <item><description>+0.0 to 1.0 based on proportion of matching non-key attributes</description></item>
    ///   <item><description>+1.0 if text values match (when not ignored)</description></item>
    /// </list>
    /// <para>Scores below 0.5 are considered non-matches during comparison.</para>
    /// </remarks>
    public class DefaultMatchingStrategy : IMatchingStrategy
    {
        // Scoring constants for clarity and easy tuning
        private const double BaseNameMatchScore = 1.0;
        private const double KeyAttributeMatchBoost = 10.0;
        private const double ValueMatchBonus = 1.0;
        private const double MinimumMatchThreshold = 0.5;

        /// <summary>
        /// Calculates a similarity score between two XML elements.
        /// </summary>
        /// <param name="e1">The first element to compare (from original document).</param>
        /// <param name="e2">The second element to compare (from new document).</param>
        /// <param name="config">The comparison configuration settings.</param>
        /// <returns>
        /// A similarity score. Returns 0.0 if either element is null or names don't match.
        /// Higher values indicate stronger matches. Scores above <see cref="MinimumMatchThreshold"/> are considered matches.
        /// </returns>
        /// <example>
        /// <code>
        /// var strategy = new DefaultMatchingStrategy();
        /// double score = strategy.Score(element1, element2, config);
        /// if (score > 0.5) { /* elements match */ }
        /// </code>
        /// </example>
        public double Score(XElement? e1, XElement? e2, XmlDiffConfig config)
        {
            if (e1 == null || e2 == null) return 0.0;

            // 1. Name Check (Namespace + LocalName) - must match according to NamespaceComparisonMode
            if (!XmlNamespaceHelper.NamespacesEqual(e1.Name, e2.Name, config.NamespaceComparisonMode))
                return 0.0;

            double score = BaseNameMatchScore;

            // 2. Key Attributes (High Priority) - get significant boost for matches
            foreach (var keyAttr in config.KeyAttributeNames)
            {
                var a1 = e1.Attribute(keyAttr)?.Value;
                var a2 = e2.Attribute(keyAttr)?.Value;

                if (a1 != null && a2 != null && a1 == a2)
                {
                    score += KeyAttributeMatchBoost;
                }
            }

            // 3. Other Attributes - proportional match score
            var attrs1 = GetFilteredAttributes(e1, config);
            var attrs2 = GetFilteredAttributes(e2, config);

            int matchingAttrs = CountMatchingAttributes(attrs1, attrs2, config);

            if (attrs1.Count > 0)
            {
                score += (double)matchingAttrs / attrs1.Count;
            }

            // 4. Value Similarity (if not ignored)
            if (!config.IgnoreValues)
            {
                // Simple check for text content if it's a leaf node or has text
                if (!e1.HasElements && !e2.HasElements)
                {
                    string v1 = XmlValueNormalizer.Normalize(e1.Value, config);
                    string v2 = XmlValueNormalizer.Normalize(e2.Value, config);
                    if (v1 == v2) score += ValueMatchBonus;
                }
            }

            return score;
        }

        /// <summary>
        /// Gets filtered attributes excluding those specified in configuration.
        /// </summary>
        private static List<XAttribute> GetFilteredAttributes(XElement element, XmlDiffConfig config)
        {
            var result = new List<XAttribute>();
            foreach (var attr in element.Attributes())
            {
                // Skip namespace declaration attributes:
                // - xmlns:prefix="uri" (attr.Name.Namespace == XNamespace.Xmlns)
                // - xmlns="uri" (attr.Name.LocalName == "xmlns" with empty namespace)
                if (attr.Name.Namespace == XNamespace.Xmlns || attr.Name.LocalName == "xmlns")
                    continue;

                if (!config.ExcludedAttributeNames.Contains(attr.Name.LocalName))
                {
                    result.Add(attr);
                }
            }
            return result;
        }

        /// <summary>
        /// Counts matching attributes between two lists, using normalized values.
        /// </summary>
        private static int CountMatchingAttributes(List<XAttribute> attrs1, List<XAttribute> attrs2, XmlDiffConfig config)
        {
            int count = 0;

            foreach (var a1 in attrs1)
            {
                string v1 = XmlValueNormalizer.Normalize(a1.Value, config);
                var a2 = attrs2.FirstOrDefault(a =>
                    a.Name == a1.Name &&
                    XmlValueNormalizer.Normalize(a.Value, config) == v1);

                if (a2 != null) count++;
            }

            return count;
        }
    }
}
