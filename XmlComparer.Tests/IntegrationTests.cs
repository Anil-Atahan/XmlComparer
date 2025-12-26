using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    /// <summary>
    /// End-to-end integration tests for complete workflows.
    /// </summary>
    public class IntegrationTests
    {
        [Fact]
        public async Task CompareContentWithReportAsync_ShouldWorkEndToEnd()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            var result = await Core.XmlComparer.CompareContentWithReportAsync(
                xml1,
                xml2,
                options => options
                    .IncludeHtml()
                    .IncludeJson()
                    .WithKeyAttributes("id"));

            Assert.NotNull(result.Diff);
            Assert.NotNull(result.Html);
            Assert.NotNull(result.Json);
            Assert.Equal(DiffType.Modified, result.Diff.Type);
        }

        [Fact]
        public void CompareContentWithReport_ShouldGenerateBothReports()
        {
            string xml1 = "<root><a><b>text</b></a></root>";
            string xml2 = "<root><a><b>changed</b></a></root>";

            var result = Core.XmlComparer.CompareContentWithReport(
                xml1,
                xml2,
                options => options
                    .IncludeHtml()
                    .IncludeJson());

            Assert.NotNull(result.Html);
            Assert.NotNull(result.Json);
            Assert.Contains("<!DOCTYPE html>", result.Html);
            Assert.Contains("\"Diff\"", result.Json);
        }

        [Fact]
        public void CompareContentWithReport_WithValidation_ShouldIncludeValidationResults()
        {
            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <xs:element name='root'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='child' type='xs:string'/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:schema>";

            string xml1 = "<root><child>value1</child></root>";
            string xml2 = "<root><child>value2</child></root>";

            string xsdFile = Path.GetTempFileName();
            string xml1File = Path.GetTempFileName();
            string xml2File = Path.GetTempFileName();

            try
            {
                File.WriteAllText(xsdFile, xsd);
                File.WriteAllText(xml1File, xml1);
                File.WriteAllText(xml2File, xml2);

                var result = Core.XmlComparer.CompareFilesWithReport(
                    xml1File,
                    xml2File,
                    options => options
                        .ValidateWithXsds(xsdFile)
                        .IncludeHtml()
                        .IncludeJson());

                Assert.NotNull(result.Validation);
                Assert.True(result.Validation.IsValid);
                Assert.NotNull(result.Diff);
                Assert.NotNull(result.Html);
                Assert.NotNull(result.Json);
            }
            finally
            {
                File.Delete(xsdFile);
                File.Delete(xml1File);
                File.Delete(xml2File);
            }
        }

        [Fact]
        public void CompareContentWithReport_ShouldGenerateCorrectSummary()
        {
            string xml1 = "<root><a/><b/><c/></root>";
            string xml2 = "<root><a/><d/><c/></root>";

            var result = Core.XmlComparer.CompareContentWithReport(
                xml1,
                xml2,
                options => options.IncludeJson());

            Assert.NotNull(result.Json);

            using var doc = JsonDocument.Parse(result.Json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Summary", out var summary));
            Assert.True(summary.TryGetProperty("Total", out var total));
            Assert.True(summary.TryGetProperty("Added", out var added));
            Assert.True(summary.TryGetProperty("Deleted", out var deleted));

            Assert.True(total.GetInt32() > 0);
            Assert.True(added.GetInt32() > 0);
            Assert.True(deleted.GetInt32() > 0);
        }

        [Fact]
        public void CompareStreams_ShouldWorkWithMemoryStreams()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            using var stream1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml1));
            using var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml2));

            var result = Core.XmlComparer.CompareStreams(stream1, stream2);

            Assert.NotNull(result);
            Assert.Equal(DiffType.Modified, result.Type);
        }

        [Fact]
        public void XmlComparerExtensions_ShouldWorkWithStringExtension()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            var result = xml1.CompareXmlToWithReport(xml2,
                options => options.IncludeJson());

            Assert.NotNull(result.Diff);
            Assert.NotNull(result.Json);
        }

        [Fact]
        public void CompareContentWithReport_WithExcludeSubtree_ShouldExcludeNode()
        {
            string xml1 = "<root><public>1</public><secret>hidden1</secret></root>";
            string xml2 = "<root><public>1</public><secret>hidden2</secret></root>";

            var result = Core.XmlComparer.CompareContentWithReport(
                xml1,
                xml2,
                options => options.ExcludeNodes(excludeSubtree: true, "secret"));

            // With excludeSubtree, the secret node should not affect diff
            Assert.Equal(DiffType.Unchanged, result.Diff.Type);
        }

        [Fact]
        public void CompareContentWithReport_WithOptionsPersistence_ShouldSaveAndLoad()
        {
            string tempFile = Path.GetTempFileName();

            try
            {
                // Create and save options
                var originalOptions = new XmlComparisonOptions();
                originalOptions
                    .WithKeyAttributes("id", "code")
                    .ExcludeAttributes("timestamp")
                    .NormalizeWhitespace()
                    .TrimValues();

                originalOptions.SaveToFile(tempFile);

                // Load options
                var loadedOptions = XmlComparisonOptions.LoadFromFile(tempFile);

                // Use loaded options - extract settings and re-apply them
                string xml1 = "<root><item id=\"1\">  text  </item></root>";
                string xml2 = "<root><item id=\"1\">text</item></root>";

                var result = Core.XmlComparer.CompareContentWithReport(
                    xml1,
                    xml2,
                    configure =>
                    {
                        foreach (var key in loadedOptions.Config.KeyAttributeNames)
                            configure.WithKeyAttributes(key);
                        foreach (var attr in loadedOptions.Config.ExcludedAttributeNames)
                            configure.ExcludingAttribute(attr);
                        if (loadedOptions.Config.NormalizeWhitespace)
                            configure.WithWhitespaceNormalization();
                        if (loadedOptions.Config.TrimValues)
                            configure.WithValueTrimming();
                    });

                // With normalization, should be unchanged
                Assert.Equal(DiffType.Unchanged, result.Diff.Type);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void CompareFilesWithReport_AllOutputTypes_ShouldGenerateAll()
        {
            string xml1 = "<root><item id=\"1\">value1</item></root>";
            string xml2 = "<root><item id=\"1\">value2</item></root>";

            string file1 = Path.GetTempFileName();
            string file2 = Path.GetTempFileName();

            try
            {
                File.WriteAllText(file1, xml1);
                File.WriteAllText(file2, xml2);

                var result = Core.XmlComparer.CompareFilesWithReport(
                    file1,
                    file2,
                    options => options
                        .IncludeHtml()
                        .IncludeJson()
                        .WithKeyAttributes("id"));

                Assert.NotNull(result.Html);
                Assert.NotNull(result.Json);
                Assert.Equal(DiffType.Modified, result.Diff.Type);

                // Verify HTML contains expected content
                Assert.Contains("html", result.Html);
                Assert.Contains("diff", result.Html.ToLower());

                // Verify JSON contains expected content
                Assert.Contains("\"Diff\"", result.Json);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        [Fact]
        public void DiffSummaryCalculator_ShouldProvideAccurateSummary()
        {
            string xml1 = "<root><a/><b/><c/></root>";
            string xml2 = "<root><a/><d/><c/><e/></root>";

            var diff = Core.XmlComparer.CompareContent(xml1, xml2);
            var summary = DiffSummaryCalculator.Compute(diff);

            Assert.True(summary.Total > 0);
            Assert.True(summary.Added > 0);    // d and e
            Assert.True(summary.Deleted > 0);   // b
            Assert.True(summary.Unchanged > 0); // a and c
        }

        [Fact]
        public async Task CompareFilesWithReportAsync_AllFeatures_ShouldWork()
        {
            string xsd = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <xs:element name='root'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='item' type='xs:string' maxOccurs='unbounded'/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:schema>";

            string xml1 = "<root><item>1</item><item>2</item></root>";
            string xml2 = "<root><item>1</item><item>3</item></root>";

            string xsdFile = Path.GetTempFileName();
            string xml1File = Path.GetTempFileName();
            string xml2File = Path.GetTempFileName();

            try
            {
                File.WriteAllText(xsdFile, xsd);
                File.WriteAllText(xml1File, xml1);
                File.WriteAllText(xml2File, xml2);

                var result = await Core.XmlComparer.CompareFilesWithReportAsync(
                    xml1File,
                    xml2File,
                    options => options
                        .ValidateWithXsds(xsdFile)
                        .IncludeHtml()
                        .IncludeJson()
                        .WithKeyAttributes("id"));

                Assert.NotNull(result.Validation);
                Assert.True(result.Validation.IsValid);
                Assert.NotNull(result.Html);
                Assert.NotNull(result.Json);
                Assert.NotNull(result.Diff);
            }
            finally
            {
                File.Delete(xsdFile);
                File.Delete(xml1File);
                File.Delete(xml2File);
            }
        }

        [Fact]
        public void CompareContent_WithCustomMatchingStrategy_ShouldUseCustomStrategy()
        {
            string xml1 = "<root><item name=\"a\"/><item name=\"b\"/></root>";
            string xml2 = "<root><item name=\"b\"/><item name=\"a\"/></root>";

            var customStrategy = new NameBasedMatchingStrategy();

            var result = Core.XmlComparer.CompareContent(
                xml1,
                xml2,
                options => options.UseMatchingStrategy(customStrategy).WithKeyAttributes("name"));

            // With custom matching strategy, should detect moves
            Assert.Equal(DiffType.Modified, result.Type);
        }

        /// <summary>
        /// Simple custom matching strategy for testing.
        /// </summary>
        private class NameBasedMatchingStrategy : IMatchingStrategy
        {
            public double Score(System.Xml.Linq.XElement? e1, System.Xml.Linq.XElement? e2, XmlDiffConfig config)
            {
                if (e1 == null || e2 == null) return 0.0;
                if (e1.Name != e2.Name) return 0.0;

                var name1 = e1.Attribute("name")?.Value;
                var name2 = e2.Attribute("name")?.Value;

                return name1 == name2 && name1 != null ? 100.0 : 0.0;
            }
        }
    }
}
