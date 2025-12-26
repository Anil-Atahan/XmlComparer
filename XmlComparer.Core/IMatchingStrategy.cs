using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a strategy for calculating similarity scores between XML elements.
    /// Implement this interface to customize how elements are matched during comparison.
    /// </summary>
    /// <remarks>
    /// <para>The scoring algorithm should return:</para>
    /// <list type="bullet">
    ///   <item><description><c>0.0</c> if elements should never match (different names, null values, etc.)</description></item>
    ///   <item><description>Values greater than <c>0.5</c> indicate a likely match</description></item>
    ///   <item><description>Higher values indicate stronger matches</description></item>
    /// </list>
    /// <para>
    /// The comparison engine uses a threshold of <c>0.5</c> to determine if two elements match.
    /// Elements with scores below this threshold are considered additions/deletions rather than matches.
    /// </para>
    /// </remarks>
    /// <example>
    /// This example shows a simple matching strategy based on element name and an "id" attribute:
    /// <code>
    /// public class IdBasedStrategy : IMatchingStrategy
    /// {
    ///     public double Score(XElement? e1, XElement? e2, XmlDiffConfig config)
    ///     {
    ///         if (e1 == null || e2 == null) return 0.0;
    ///         if (e1.Name != e2.Name) return 0.0;
    ///
    ///         var id1 = e1.Attribute("id")?.Value;
    ///         var id2 = e2.Attribute("id")?.Value;
    ///
    ///         return id1 == id2 && id1 != null ? 100.0 : 0.0;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="DefaultMatchingStrategy"/>
    public interface IMatchingStrategy
    {
        /// <summary>
        /// Calculates a similarity score between two XML elements.
        /// </summary>
        /// <param name="e1">The first element to compare (typically from the original document).</param>
        /// <param name="e2">The second element to compare (typically from the new document).</param>
        /// <param name="config">The comparison configuration settings.</param>
        /// <returns>
        /// A similarity score between 0.0 and any positive value.
        /// Returns 0.0 if either element is null or if they should not match.
        /// </returns>
        /// <remarks>
        /// <para>Implementation guidelines:</para>
        /// <list type="bullet">
        ///   <item><description>Always return 0.0 for null elements</description></item>
        ///   <item><description>Return 0.0 if element names don't match</description></item>
        ///   <item><description>Use key attributes for primary matching criteria</description></item>
        ///   <item><description>Consider attribute similarity for secondary scoring</description></item>
        ///   <item><description>Use value comparison as a tertiary factor</description></item>
        /// </list>
        /// </remarks>
        double Score(XElement? e1, XElement? e2, XmlDiffConfig config);
    }
}
