using System.Text.Json;
using System.Xml.Linq;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class JsonDiffFormatterTests
    {
        [Fact]
        public void GenerateJson_ShouldExcludeIgnoredAttributes()
        {
            var config = new XmlDiffConfig();
            config.ExcludedAttributeNames.Add("secret");
            var service = new XmlComparerService(config);

            string xml1 = "<root id='1' secret='x'></root>";
            string xml2 = "<root id='1' secret='y'></root>";

            var diff = service.CompareXml(xml1, xml2);
            string json = service.GenerateJson(diff);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var diffElement = root.GetProperty("Diff");
            var attrs = diffElement.GetProperty("OriginalAttributes");

            Assert.True(attrs.TryGetProperty("id", out _));
            Assert.False(attrs.TryGetProperty("secret", out _));
            Assert.Equal("Unchanged", diffElement.GetProperty("Type").GetString());
        }

        [Fact]
        public void GenerateJson_ShouldOmitExcludedNodeValuesButKeepStructure()
        {
            var config = new XmlDiffConfig();
            config.ExcludedNodeNames.Add("secret");
            var service = new XmlComparerService(config);

            string xml1 = "<root><secret><child>1</child></secret></root>";
            string xml2 = "<root><secret><child>2</child></secret></root>";

            var diff = service.CompareXml(xml1, xml2);
            string json = service.GenerateJson(diff);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var diffElement = root.GetProperty("Diff");
            var secret = diffElement.GetProperty("Children")[0];

            Assert.False(secret.TryGetProperty("OriginalValue", out _));
            Assert.False(secret.TryGetProperty("NewValue", out _));
            Assert.True(secret.TryGetProperty("Children", out _));
        }

        [Fact]
        public void GenerateJson_ShouldIncludeNamespaceInName()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            XNamespace ns = "urn:test";
            string xml1 = $"<root xmlns=\"{ns}\"><item/></root>";
            string xml2 = $"<root xmlns=\"{ns}\"><item/></root>";

            var diff = service.CompareXml(xml1, xml2);
            string json = service.GenerateJson(diff);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var diffElement = root.GetProperty("Diff");

            Assert.Equal("{urn:test}root", diffElement.GetProperty("Name").GetString());
        }

        [Fact]
        public void GenerateJson_ShouldIncludeValidationMetadataWhenProvided()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            string xml1 = "<root></root>";
            string xml2 = "<root></root>";

            var diff = service.CompareXml(xml1, xml2);
            var validation = new XmlValidationResult();
            validation.Errors.Add(new XmlValidationError("bad", 1, 2));

            string json = service.GenerateJson(diff, validation);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Validation", out var validationElem));
            Assert.False(validationElem.GetProperty("IsValid").GetBoolean());
            Assert.True(root.TryGetProperty("Diff", out _));
            Assert.True(root.TryGetProperty("Summary", out _));
        }

        [Fact]
        public void GenerateValidationJson_ShouldOnlyContainValidationBlock()
        {
            var service = new XmlComparerService(new XmlDiffConfig());
            var validation = new XmlValidationResult();
            validation.Errors.Add(new XmlValidationError("bad", 1, 2));

            string json = service.GenerateValidationJson(validation);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Validation", out _));
            Assert.False(root.TryGetProperty("Diff", out _));
        }
    }
}
