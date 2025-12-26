using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    /// <summary>
    /// Tests for edge cases and unusual but valid XML scenarios.
    /// </summary>
    public class EdgeCaseTests
    {
        [Fact]
        public void CompareXml_ShouldHandleCDataSections()
        {
            string xml1 = "<root><![CDATA[Hello <world>]]></root>";
            string xml2 = "<root><![CDATA[Hello <world>]]></root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldDetectCDataChanges()
        {
            string xml1 = "<root><![CDATA[Hello]]></root>";
            string xml2 = "<root><![CDATA[Goodbye]]></root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Modified, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleComments()
        {
            string xml1 = "<root><!-- comment --><child/></root>";
            string xml2 = "<root><child/></root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            // Comments are stripped during comparison
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleProcessingInstructions()
        {
            string xml1 = "<?pi target?><root/>";
            string xml2 = "<root/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleSelfClosingVsEmptyTags()
        {
            string xml1 = "<root><child/></root>";
            string xml2 = "<root><child></child></root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            // XDocument treats these as equivalent
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleMixedContent()
        {
            string xml1 = "<root>Text before<child/>Text after</root>";
            string xml2 = "<root>Text before<child/>Text after</root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleUnicodeCharacters()
        {
            string xml1 = "<root>Hello 世界 🌍</root>";
            string xml2 = "<root>Hello 世界 🌍</root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleSurrogatePairs()
        {
            // Emoji that uses surrogate pairs
            string xml1 = "<root>𠮷𠮸</root>";
            string xml2 = "<root>𠮷𠮸</root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleDefaultNamespaceChange()
        {
            string xml1 = "<root xmlns=\"urn:old\"><child/></root>";
            string xml2 = "<root xmlns=\"urn:new\"><child/></root>";

            // Use UriSensitive mode to detect namespace URI changes
            var config = new XmlDiffConfig { NamespaceComparisonMode = NamespaceComparisonMode.UriSensitive };
            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // Namespace change should be detected
            Assert.Equal(DiffType.NamespaceChanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleNamespacePrefixChange()
        {
            string xml1 = "<ns:root xmlns:ns=\"urn:test\"><ns:child/></ns:root>";
            string xml2 = "<root xmlns=\"urn:test\"><child/></root>";

            // Use UriSensitive mode to treat same namespace URIs as equivalent
            var config = new XmlDiffConfig { NamespaceComparisonMode = NamespaceComparisonMode.UriSensitive };
            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // Semantically equivalent (same namespace URI)
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleAttributesInDifferentOrder()
        {
            string xml1 = "<root a=\"1\" b=\"2\" c=\"3\"/>";
            string xml2 = "<root c=\"3\" a=\"1\" b=\"2\"/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            // Attribute order shouldn't matter
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleEmptyAttributeValues()
        {
            string xml1 = "<root attr=\"\"/>";
            string xml2 = "<root attr=\"\"/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldDetectEmptyAttributeVsNoAttribute()
        {
            string xml1 = "<root attr=\"\"/>";
            string xml2 = "<root/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            // Empty attribute vs no attribute is different
            Assert.Equal(DiffType.Modified, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleSpecialCharactersInContent()
        {
            string xml1 = "<root><![CDATA[<>&&apos;&quot;\r\n\t]]></root>";
            string xml2 = "<root><![CDATA[<>&&apos;&quot;\r\n\t]]></root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleVeryLongAttributeValues()
        {
            string longValue = new string('x', 10000);
            string xml1 = $"<root attr=\"{longValue}\"/>";
            string xml2 = $"<root attr=\"{longValue}\"/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleDeepNesting()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                sb.Append("<level>");
            }
            sb.Append("value");
            for (int i = 0; i < 100; i++)
            {
                sb.Append("</level>");
            }

            string xml = sb.ToString();

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml, xml);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleManyChildren()
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < 1000; i++)
            {
                sb.AppendFormat("<item id=\"{0}\">value{0}</item>", i);
            }
            sb.Append("</root>");

            string xml = sb.ToString();

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml, xml);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleNamespacedAttributes()
        {
            string xml1 = "<root xmlns:ns=\"urn:test\" ns:attr=\"value\"/>";
            string xml2 = "<root xmlns:ns=\"urn:test\" ns:attr=\"value\"/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_WithKeyAttributes_ShouldDetectMovedElements()
        {
            string xml1 = "<root><item id=\"1\"/><item id=\"2\"/><item id=\"3\"/></root>";
            string xml2 = "<root><item id=\"2\"/><item id=\"3\"/><item id=\"1\"/></root>";

            var config = new XmlDiffConfig();
            config.KeyAttributeNames.Add("id");

            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // With key attributes, moved elements should be detected
            Assert.Equal(DiffType.Modified, diff.Type);

            // Check for Moved type in children
            var hasMoved = HasDiffTypeInChildren(diff, DiffType.Moved);
            Assert.True(hasMoved, "Expected to find Moved diff type with key attributes");
        }

        [Fact]
        public void CompareXml_WithNormalization_ShouldNormalizeWhitespace()
        {
            string xml1 = "<root>  Text   with    spaces  </root>";
            string xml2 = "<root>Text with spaces</root>";

            var config = new XmlDiffConfig
            {
                NormalizeWhitespace = true,
                TrimValues = true
            };

            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // With normalization, should be unchanged
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_WithExcludeSubtree_ShouldIgnoreExcludedNodes()
        {
            string xml1 = "<root><data><secret>hidden1</secret></data></root>";
            string xml2 = "<root><data><secret>hidden2</secret></data></root>";

            var config = new XmlDiffConfig
            {
                ExcludeSubtree = true
            };
            config.ExcludedNodeNames.Add("secret");

            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // Excluded subtree should be ignored
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_WithIgnoreValues_ShouldOnlyCompareStructure()
        {
            string xml1 = "<root><item>value1</item></root>";
            string xml2 = "<root><item>value2</item></root>";

            var config = new XmlDiffConfig
            {
                IgnoreValues = true
            };

            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // With ignore values, only structure is compared
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_WithExcludeAttributes_ShouldIgnoreExcludedAttributes()
        {
            string xml1 = "<root><item id=\"1\" timestamp=\"2023-01-01\"/></root>";
            string xml2 = "<root><item id=\"1\" timestamp=\"2023-01-02\"/></root>";

            var config = new XmlDiffConfig();
            config.ExcludedAttributeNames.Add("timestamp");

            var service = new XmlComparerService(config);
            var diff = service.CompareXml(xml1, xml2);

            // Excluded attributes should be ignored
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        private bool HasDiffTypeInChildren(DiffMatch diff, DiffType type)
        {
            if (diff.Type == type) return true;
            foreach (var child in diff.Children)
            {
                if (HasDiffTypeInChildren(child, type)) return true;
            }
            return false;
        }

        [Fact]
        public void CompareXml_ShouldHandleEmptyRoot()
        {
            string xml1 = "<root/>";
            string xml2 = "<root></root>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleXmlDeclaration()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><root/>";
            string xml2 = "<root/>";

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(xml1, xml2);

            // XML declaration is not part of XDocument
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }
    }
}
