using System;
using System.Text;
using System.Xml.Linq;

namespace XmlComparer.Tests.Helpers
{
    /// <summary>
    /// Helper class for building test XML documents with various structures.
    /// </summary>
    public static class TestDataBuilder
    {
        /// <summary>
        /// Builds a simple XML document with specified depth and children per level.
        /// </summary>
        public static string BuildXml(int depth = 1, int childrenPerLevel = 1)
        {
            return BuildXmlElement("root", depth, childrenPerLevel);
        }

        private static string BuildXmlElement(string name, int remainingDepth, int children)
        {
            var sb = new StringBuilder();
            sb.Append($"<{name}>");

            if (remainingDepth > 0)
            {
                for (int i = 0; i < children; i++)
                {
                    sb.Append(BuildXmlElement("child", remainingDepth - 1, children));
                }
            }
            else
            {
                sb.Append("value");
            }

            sb.Append($"</{name}>");
            return sb.ToString();
        }

        /// <summary>
        /// Builds XML with attributes.
        /// </summary>
        public static string BuildXmlWithAttributes(int attributeCount = 5)
        {
            var sb = new StringBuilder();
            sb.Append("<root");

            for (int i = 1; i <= attributeCount; i++)
            {
                sb.Append($" attr{i}=\"value{i}\"");
            }

            sb.Append(">content</root>");
            return sb.ToString();
        }

        /// <summary>
        /// Builds XML with namespaces.
        /// </summary>
        public static string BuildXmlWithNamespaces()
        {
            return @"<root xmlns=""urn:default"" xmlns:ns1=""urn:ns1"">
                <child ns1:attr=""value"">content</child>
            </root>";
        }

        /// <summary>
        /// Builds XML with special characters.
        /// </summary>
        public static string BuildXmlWithSpecialChars()
        {
            return @"<root><![CDATA[<>&'""]]>
                <!-- comment -->
                <child attr=""<>""&"">value</child>
            </root>";
        }

        /// <summary>
        /// Builds XML with CDATA section.
        /// </summary>
        public static string BuildXmlWithCData(string content = "Hello World")
        {
            return $"<root><![CDATA[{content}]]></root>";
        }

        /// <summary>
        /// Builds XML with mixed content (text and elements).
        /// </summary>
        public static string BuildXmlWithMixedContent()
        {
            return "<root>Text before<child/>Text after</root>";
        }

        /// <summary>
        /// Builds deeply nested XML.
        /// </summary>
        public static string BuildXmlDeep(int depth)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                sb.Append("<level>");
            }
            sb.Append("value");
            for (int i = 0; i < depth; i++)
            {
                sb.Append("</level>");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Builds wide XML with many siblings.
        /// </summary>
        public static string BuildXmlWide(int width)
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < width; i++)
            {
                sb.AppendFormat("<item id=\"{0}\">value{0}</item>", i);
            }
            sb.Append("</root>");
            return sb.ToString();
        }

        /// <summary>
        /// Builds XML with key attributes for matching.
        /// </summary>
        public static string BuildXmlWithKeyAttributes(int count)
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < count; i++)
            {
                sb.AppendFormat("<item id=\"{0}\" name=\"name{0}\">value{0}</item>", i);
            }
            sb.Append("</root>");
            return sb.ToString();
        }
    }
}
