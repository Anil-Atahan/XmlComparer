using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class XmlComparerClientTests
    {
        [Fact]
        public void CompareContentWithReport_ShouldProduceHtmlAndJson()
        {
            var client = new XmlComparerClient(new XmlDiffConfig());
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            var result = client.CompareContentWithReport(
                xml1,
                xml2,
                options => options.IncludeHtml().IncludeJson());

            Assert.NotNull(result.Diff);
            Assert.NotNull(result.Html);
            Assert.NotNull(result.Json);
        }

        [Fact]
        public void CompareXmlFileToWithReport_Extension_ShouldWork()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            string file1 = Path.GetTempFileName();
            string file2 = Path.GetTempFileName();
            File.WriteAllText(file1, xml1);
            File.WriteAllText(file2, xml2);

            try
            {
                var result = file1.CompareXmlFileToWithReport(
                    file2,
                    options => options.IncludeJson());

                Assert.NotNull(result.Diff);
                Assert.NotNull(result.Json);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        [Fact]
        public void Options_ToJsonAndFromJson_ShouldRoundTrip()
        {
            var options = new XmlComparisonOptions()
                .WithKeyAttributes("id")
                .ExcludeAttributes("timestamp")
                .ExcludeNodes(excludeSubtree: true, "metadata")
                .ValidateWithXsds("schema.xsd")
                .IncludeHtml()
                .IncludeJson()
                .UseMatchingStrategy(new DefaultMatchingStrategy(), "default");

            string json = options.ToJson();
            var roundTripped = XmlComparisonOptions.FromJson(json);

            Assert.Contains("id", roundTripped.Config.KeyAttributeNames);
            Assert.Contains("timestamp", roundTripped.Config.ExcludedAttributeNames);
            Assert.Contains("metadata", roundTripped.Config.ExcludedNodeNames);
            Assert.True(roundTripped.Config.ExcludeSubtree);
            Assert.True(roundTripped.GenerateHtml);
            Assert.True(roundTripped.GenerateJson);
            Assert.Contains("schema.xsd", roundTripped.XsdPaths);
            Assert.Equal("default", roundTripped.MatchingStrategyId);
        }

        [Fact]
        public void Options_ResolveMatchingStrategy_ShouldSetStrategy()
        {
            var options = new XmlComparisonOptions()
                .UseMatchingStrategy(new DefaultMatchingStrategy(), "default");

            string json = options.ToJson();
            var loaded = XmlComparisonOptions.FromJson(json);

            loaded.ResolveMatchingStrategy(id => id == "default" ? new DefaultMatchingStrategy() : null);

            Assert.NotNull(loaded.MatchingStrategy);
        }

        [Fact]
        public void Options_ResolveMatchingStrategyFromRegistry_ShouldSetStrategy()
        {
            XmlComparisonOptions.MatchingStrategyRegistry.Clear();
            Assert.True(XmlComparisonOptions.MatchingStrategyRegistry.TryRegister(
                "default",
                () => new DefaultMatchingStrategy()));

            var options = new XmlComparisonOptions()
                .UseMatchingStrategy(new DefaultMatchingStrategy(), "default");

            string json = options.ToJson();
            var loaded = XmlComparisonOptions.FromJson(json);

            loaded.ResolveMatchingStrategyFromRegistry();

            Assert.NotNull(loaded.MatchingStrategy);
            Assert.True(XmlComparisonOptions.MatchingStrategyRegistry.Unregister("default"));
        }

        [Fact]
        public void Registry_TryResolve_ShouldReturnStrategy()
        {
            XmlComparisonOptions.MatchingStrategyRegistry.Clear();
            Assert.True(XmlComparisonOptions.MatchingStrategyRegistry.TryRegister(
                "default",
                () => new DefaultMatchingStrategy()));

            Assert.True(XmlComparisonOptions.MatchingStrategyRegistry.TryResolve("default", out var strategy));
            Assert.NotNull(strategy);
        }

        [Fact]
        public void Options_SaveAndLoad_ShouldRoundTrip()
        {
            var options = new XmlComparisonOptions()
                .WithKeyAttributes("id")
                .IncludeHtml();

            string path = Path.GetTempFileName();
            try
            {
                options.SaveToFile(path);
                var loaded = XmlComparisonOptions.LoadFromFile(path);

                Assert.Contains("id", loaded.Config.KeyAttributeNames);
                Assert.True(loaded.GenerateHtml);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Options_FromJson_ShouldRejectFutureVersion()
        {
            string json = "{ \"Version\": 2 }";
            Assert.Throws<InvalidOperationException>(() => XmlComparisonOptions.FromJson(json));
        }

        [Fact]
        public void Options_FromJson_ShouldTreatMissingVersionAsV1()
        {
            string json = "{ \"GenerateHtml\": true }";
            var options = XmlComparisonOptions.FromJson(json);

            Assert.True(options.GenerateHtml);
        }

        [Fact]
        public void Options_FromJson_WithWarning_ShouldInvokeHandlerWhenMissingVersion()
        {
            string json = "{ \"Version\": 0, \"GenerateHtml\": true }";
            bool warned = false;

            var options = XmlComparisonOptions.FromJson(json, _ => warned = true);

            Assert.True(warned);
            Assert.True(options.GenerateHtml);
        }

        [Fact]
        public async Task CompareFilesAsync_ShouldWork()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            string file1 = Path.GetTempFileName();
            string file2 = Path.GetTempFileName();
            File.WriteAllText(file1, xml1);
            File.WriteAllText(file2, xml2);

            try
            {
                var client = new XmlComparerClient(new XmlDiffConfig());
                var diff = await client.CompareFilesAsync(file1, file2);
                Assert.NotNull(diff);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        [Fact]
        public void CompareStreams_ShouldWork()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(xml1));
            using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(xml2));

            var client = new XmlComparerClient(new XmlDiffConfig());
            var diff = client.CompareStreams(stream1, stream2);

            Assert.NotNull(diff);
        }

        [Fact]
        public void CompareContentWithReport_ShouldOmitValidationWhenDisabled()
        {
            string xsd = @"
<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:element name='root'>
    <xs:complexType>
      <xs:sequence>
        <xs:element name='child' type='xs:int' />
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";

            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            string xsdPath = Path.GetTempFileName();
            File.WriteAllText(xsdPath, xsd);

            try
            {
                var result = XmlComparer.Core.XmlComparer.CompareContentWithReport(
                    xml1,
                    xml2,
                    options => options.ValidateWithXsds(xsdPath).IncludeJson(includeValidation: false));

                using var doc = JsonDocument.Parse(result.Json!);
                var root = doc.RootElement;

                Assert.True(root.TryGetProperty("Diff", out _));
                Assert.False(root.TryGetProperty("Validation", out _));
            }
            finally
            {
                File.Delete(xsdPath);
            }
        }
    }
}
