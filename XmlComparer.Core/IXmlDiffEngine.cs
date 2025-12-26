using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a contract for XML diff engines that compare XML documents.
    /// </summary>
    public interface IXmlDiffEngine
    {
        /// <summary>
        /// Compares two XML elements and returns the root of the diff tree.
        /// </summary>
        /// <param name="originalRoot">The root element of the original XML document.</param>
        /// <param name="newRoot">The root element of the new XML document.</param>
        /// <returns>A DiffMatch representing the root of the diff tree.</returns>
        DiffMatch Compare(XElement? originalRoot, XElement? newRoot);
    }
}
