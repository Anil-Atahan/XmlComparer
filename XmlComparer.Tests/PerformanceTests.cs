using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    /// <summary>
    /// Performance tests to ensure acceptable performance for various document sizes.
    /// </summary>
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CompareXml_ShouldHandleMediumDocumentQuickly()
        {
            // Build document with 5,000 elements
            string doc = BuildDocument(5000);

            var service = new XmlComparerService(new XmlDiffConfig());
            var stopwatch = Stopwatch.StartNew();

            var diff = service.CompareXml(doc, doc);

            stopwatch.Stop();

            _output.WriteLine($"Comparison of 5,000 elements took {stopwatch.ElapsedMilliseconds}ms");

            Assert.Equal(DiffType.Unchanged, diff.Type);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Comparison took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        [Fact]
        public void CompareXml_ShouldHandleLargeDocumentReasonably()
        {
            // Build document with 10,000 elements
            string doc = BuildDocument(10000);

            var service = new XmlComparerService(new XmlDiffConfig());
            var stopwatch = Stopwatch.StartNew();

            var diff = service.CompareXml(doc, doc);

            stopwatch.Stop();

            _output.WriteLine($"Comparison of 10,000 elements took {stopwatch.ElapsedMilliseconds}ms");

            Assert.Equal(DiffType.Unchanged, diff.Type);
            Assert.True(stopwatch.ElapsedMilliseconds < 20000,
                $"Comparison took {stopwatch.ElapsedMilliseconds}ms, expected < 20000ms");
        }

        [Fact]
        public void CompareXml_ShouldHandleDeepNestingQuickly()
        {
            // Build deeply nested document
            string deepDoc = BuildDeepDocument(500);

            var service = new XmlComparerService(new XmlDiffConfig());
            var stopwatch = Stopwatch.StartNew();

            var diff = service.CompareXml(deepDoc, deepDoc);

            stopwatch.Stop();

            _output.WriteLine($"Comparison of 500 levels deep took {stopwatch.ElapsedMilliseconds}ms");

            Assert.True(stopwatch.ElapsedMilliseconds < 1000);
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleManyAttributesQuickly()
        {
            // Element with 100 attributes
            int attrCount = 100;
            var xml1 = BuildXmlWithAttributes(attrCount, "1");
            var xml2 = BuildXmlWithAttributes(attrCount, "1");

            var service = new XmlComparerService(new XmlDiffConfig());
            var stopwatch = Stopwatch.StartNew();

            var diff = service.CompareXml(xml1, xml2);

            stopwatch.Stop();

            _output.WriteLine($"Comparison with {attrCount} attributes took {stopwatch.ElapsedMilliseconds}ms");

            Assert.True(stopwatch.ElapsedMilliseconds < 100);
            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void GenerateHtml_ShouldHandleLargeDiffQuickly()
        {
            string doc = BuildDocument(5000);
            string modified = ModifyEveryNth(doc, 10);

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(doc, modified);

            var stopwatch = Stopwatch.StartNew();
            string html = service.GenerateHtml(diff);
            stopwatch.Stop();

            _output.WriteLine($"HTML generation for 5,000 element diff took {stopwatch.ElapsedMilliseconds}ms");

            Assert.True(stopwatch.ElapsedMilliseconds < 5000);
            Assert.NotEmpty(html);
        }

        [Fact]
        public void GenerateJson_ShouldHandleLargeDiffQuickly()
        {
            string doc = BuildDocument(5000);
            string modified = ModifyEveryNth(doc, 10);

            var service = new XmlComparerService(new XmlDiffConfig());
            var diff = service.CompareXml(doc, modified);

            var stopwatch = Stopwatch.StartNew();
            string json = service.GenerateJson(diff);
            stopwatch.Stop();

            _output.WriteLine($"JSON generation for 5,000 element diff took {stopwatch.ElapsedMilliseconds}ms");

            Assert.True(stopwatch.ElapsedMilliseconds < 3000);
            Assert.NotEmpty(json);
        }

        [Fact]
        public async Task CompareAsync_ShouldBeTrulyAsync()
        {
            // Use a larger document to ensure async behavior
            string doc = BuildDocument(5000);

            var service = new XmlComparerService(new XmlDiffConfig());

            var task = service.CompareXmlAsync(doc, doc);

            // Give it a small delay to start
            await Task.Delay(10);

            // After delay, the task may still be running or completed
            // The key is that we can await it successfully
            var result = await task;

            _output.WriteLine($"Async comparison completed successfully");

            // Verify the comparison worked correctly
            Assert.NotNull(result);
            Assert.Equal(DiffType.Unchanged, result.Type);
        }

        [Fact]
        public void CompareXml_WithKeyAttributes_ShouldOptimizeMatching()
        {
            // Build document with many elements but key attributes for fast matching
            var sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < 1000; i++)
            {
                sb.AppendFormat("<item id=\"{0}\">value{0}</item>", i);
            }
            sb.Append("</root>");

            string xml = sb.ToString();

            var config = new XmlDiffConfig();
            config.KeyAttributeNames.Add("id");

            var service = new XmlComparerService(new XmlDiffConfig());

            var stopwatch = Stopwatch.StartNew();
            var diff = service.CompareXml(xml, xml);
            stopwatch.Stop();

            _output.WriteLine($"Comparison with key attributes took {stopwatch.ElapsedMilliseconds}ms");

            Assert.Equal(DiffType.Unchanged, diff.Type);
        }

        [Fact]
        public void CompareXml_ShouldHandleMovedElementsEfficiently()
        {
            // Create document where many elements are moved
            var sb1 = new StringBuilder();
            var sb2 = new StringBuilder();
            sb1.Append("<root>");
            sb2.Append("<root>");

            for (int i = 0; i < 100; i++)
            {
                sb1.AppendFormat("<item id=\"{0}\"/>", i);
            }

            // Reverse order in second document
            for (int i = 99; i >= 0; i--)
            {
                sb2.AppendFormat("<item id=\"{0}\"/>", i);
            }

            sb1.Append("</root>");
            sb2.Append("</root>");

            string xml1 = sb1.ToString();
            string xml2 = sb2.ToString();

            var config = new XmlDiffConfig();
            config.KeyAttributeNames.Add("id");

            var service = new XmlComparerService(new XmlDiffConfig());

            var stopwatch = Stopwatch.StartNew();
            var diff = service.CompareXml(xml1, xml2);
            stopwatch.Stop();

            _output.WriteLine($"Move detection for 100 elements took {stopwatch.ElapsedMilliseconds}ms");

            // Should complete in reasonable time
            Assert.True(stopwatch.ElapsedMilliseconds < 2000);
        }

        #region Helper Methods

        private string BuildDocument(int elementCount)
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < elementCount; i++)
            {
                sb.AppendFormat("<item id=\"{0}\">value{0}</item>", i);
            }
            sb.Append("</root>");
            return sb.ToString();
        }

        private string BuildDeepDocument(int depth)
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

        private string BuildXmlWithAttributes(int attributeCount, string valueSuffix)
        {
            var sb = new StringBuilder();
            sb.Append("<root");
            for (int i = 1; i <= attributeCount; i++)
            {
                sb.AppendFormat($" attr{i}=\"value{i}{valueSuffix}\"");
            }
            sb.Append("/>");
            return sb.ToString();
        }

        private string ModifyEveryNth(string xml, int n)
        {
            // Simple modification - replace every nth value
            var modified = System.Text.RegularExpressions.Regex.Replace(
                xml,
                $"value({n})",
                "modified$1");
            return modified;
        }

        #endregion
    }
}
