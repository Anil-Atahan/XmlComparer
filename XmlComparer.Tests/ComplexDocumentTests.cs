using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class ComplexDocumentTests
    {
        [Fact]
        public void ComplexDocs_ShouldValidateAgainstSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string schemaResource = "XmlComparer.Tests.Resources.ComplexSchema.xsd";
            string doc1 = ReadResource(assembly, "XmlComparer.Tests.Resources.ComplexDoc1.xml");
            string doc2 = ReadResource(assembly, "XmlComparer.Tests.Resources.ComplexDoc2.xml");

            var schemaSet = XmlSchemaSetFactory.FromEmbeddedResource(assembly, schemaResource);
            var validator = new XmlSchemaValidator(schemaSet);

            var result1 = validator.ValidateContent(doc1);
            var result2 = validator.ValidateContent(doc2);

            Assert.True(result1.IsValid);
            Assert.True(result2.IsValid);
        }

        [Fact]
        public void ComplexDocs_ShouldProduceDiffAndReports()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string doc1 = ReadResource(assembly, "XmlComparer.Tests.Resources.ComplexDoc1.xml");
            string doc2 = ReadResource(assembly, "XmlComparer.Tests.Resources.ComplexDoc2.xml");

            var result = XmlComparer.Core.XmlComparer.CompareContentWithReport(
                doc1,
                doc2,
                options => options
                    .WithKeyAttributes("id", "code", "key")
                    .IncludeHtml()
                    .IncludeJson());

            Assert.NotNull(result.Diff);
            Assert.Equal(DiffType.Modified, result.Diff.Type);
            Assert.NotNull(result.Html);
            Assert.NotNull(result.Json);

            using var doc = JsonDocument.Parse(result.Json!);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("Diff", out _));
        }

        [Fact]
        public void LargeDocs_ShouldProduceDiff()
        {
            string doc1 = BuildLargeDocument(300, 5);
            string doc2 = BuildLargeDocument(300, 5, modifyEvery: 37);

            var result = XmlComparer.Core.XmlComparer.CompareContentWithReport(
                doc1,
                doc2,
                options => options
                    .WithKeyAttributes("id")
                    .IncludeJson());

            Assert.NotNull(result.Diff);
            Assert.Equal(DiffType.Modified, result.Diff.Type);
            Assert.NotNull(result.Json);
        }

        [Fact]
        public void VeryLargeDocs_ShouldProduceDiff()
        {
            string doc1 = BuildLargeDocument(2000, 10);
            string doc2 = BuildLargeDocument(2000, 10, modifyEvery: 101);

            var result = XmlComparer.Core.XmlComparer.CompareContentWithReport(
                doc1,
                doc2,
                options => options
                    .WithKeyAttributes("id")
                    .IncludeJson());

            Assert.NotNull(result.Diff);
            Assert.Equal(DiffType.Modified, result.Diff.Type);
            Assert.NotNull(result.Json);
        }

        private static string ReadResource(Assembly assembly, string name)
        {
            using Stream? stream = assembly.GetManifestResourceStream(name);
            if (stream == null) throw new FileNotFoundException($"Resource not found: {name}", name);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string BuildLargeDocument(int itemCount, int attrPerItem, int modifyEvery = -1)
        {
            var sb = new StringBuilder();
            sb.Append("<root>");
            for (int i = 0; i < itemCount; i++)
            {
                sb.Append("<item id=\"");
                sb.Append(i);
                sb.Append("\"");
                for (int a = 0; a < attrPerItem; a++)
                {
                    sb.Append(" a");
                    sb.Append(a);
                    sb.Append("=\"");
                    sb.Append((i + a) % 10);
                    sb.Append("\"");
                }
                sb.Append(">");
                sb.Append("value");
                sb.Append(i);
                if (modifyEvery > 0 && i % modifyEvery == 0)
                {
                    sb.Append("-changed");
                }
                sb.Append("</item>");
            }
            sb.Append("</root>");
            return sb.ToString();
        }
    }
}
