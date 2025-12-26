using System.Xml.Linq;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class XmlComparerServiceTests
    {
        [Fact]
        public void CompareXml_ShouldDetectAddedElement()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            string xml1 = "<root></root>";
            string xml2 = "<root><child/></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Modified, diff.Type); // Root is modified because child added
            Assert.Single(diff.Children);
            Assert.Equal(DiffType.Added, diff.Children[0].Type);
        }

        [Fact]
        public void CompareXml_ShouldDetectDeletedElement()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            string xml1 = "<root><child/></root>";
            string xml2 = "<root></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Modified, diff.Type);
            Assert.Single(diff.Children);
            Assert.Equal(DiffType.Deleted, diff.Children[0].Type);
        }
        
        [Fact]
        public void CompareXml_ShouldDetectMovedElement()
        {
            var config = new XmlDiffConfig();
            config.KeyAttributeNames.Add("id");
            var service = new XmlComparerService(config);
            
            string xml1 = "<root><a id='1'/><b id='2'/></root>";
            string xml2 = "<root><b id='2'/><a id='1'/></root>";

            var diff = service.CompareXml(xml1, xml2);

            // Both children should be matched but one or both marked as Moved depending on LCS
            // LCS of [A, B] and [B, A] is length 1 (either A or B).
            // So one is Unchanged (part of LCS) and one is Moved.
            
            Assert.Contains(diff.Children, c => c.Type == DiffType.Moved);
        }

        [Fact]
        public void CompareXml_ShouldIgnoreExcludedNodes()
        {
            var config = new XmlDiffConfig();
            config.ExcludedNodeNames.Add("secret");
            var service = new XmlComparerService(config);

            string xml1 = "<root><secret>1</secret></root>";
            string xml2 = "<root><secret>2</secret></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
            Assert.Single(diff.Children);
            Assert.Equal(DiffType.Unchanged, diff.Children[0].Type);
        }

        [Fact]
        public void CompareXml_ShouldAllowTransparentExclude()
        {
            var config = new XmlDiffConfig();
            config.ExcludedNodeNames.Add("secret");
            var service = new XmlComparerService(config);

            string xml1 = "<root><secret><child>1</child></secret></root>";
            string xml2 = "<root><secret><child>2</child></secret></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Modified, diff.Type);
            Assert.Single(diff.Children);
            Assert.Equal(DiffType.Modified, diff.Children[0].Type);
        }

        [Fact]
        public void CompareXml_ShouldExcludeSubtreeWhenConfigured()
        {
            var config = new XmlDiffConfig { ExcludeSubtree = true };
            config.ExcludedNodeNames.Add("secret");
            var service = new XmlComparerService(config);

            string xml1 = "<root><secret><child>1</child></secret></root>";
            string xml2 = "<root><secret><child>2</child></secret></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
            Assert.Empty(diff.Children);
        }

        [Fact]
        public void CompareXml_ShouldBuildIndexedPathsForSiblings()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            string xml1 = "<root><item/><item/></root>";
            string xml2 = "<root><item/><item/></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(2, diff.Children.Count);
            Assert.NotEqual(diff.Children[0].Path, diff.Children[1].Path);
            Assert.Contains("/root[1]/item[1]", diff.Children[0].Path);
        }

        [Fact]
        public void CompareXml_ShouldIncludeNamespaceInPaths()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            XNamespace ns = "urn:test";
            string xml1 = $"<root xmlns=\"{ns}\"><item/></root>";
            string xml2 = $"<root xmlns=\"{ns}\"><item/></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Contains("/{urn:test}root[1]/{urn:test}item[1]", diff.Children[0].Path);
        }

        [Fact]
        public void CompareXml_ShouldNormalizeWhitespaceAndNewlines()
        {
            var config = new XmlDiffConfig
            {
                NormalizeWhitespace = true,
                NormalizeNewlines = true,
                TrimValues = true
            };
            var service = new XmlComparerService(config);

            string xml1 = "<root><value>Line1\r\nLine2</value><attr a=' a  b '/></root>";
            string xml2 = "<root><value>Line1\nLine2</value><attr a='a b'/></root>";

            var diff = service.CompareXml(xml1, xml2);

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }
    }
}
