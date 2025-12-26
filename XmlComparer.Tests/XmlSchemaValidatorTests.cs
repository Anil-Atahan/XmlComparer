using System.Collections.Generic;
using System.IO;
using System.Xml;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class XmlSchemaValidatorTests
    {
        [Fact]
        public void ValidateContent_ShouldFailForInvalidXml()
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

            string xml = "<root><child>not-an-int</child></root>";

            string xsdPath = Path.GetTempFileName();
            File.WriteAllText(xsdPath, xsd);

            try
            {
                var validator = new XmlSchemaValidator(new List<string> { xsdPath });
                var result = validator.ValidateContent(xml);

                Assert.False(result.IsValid);
                Assert.NotEmpty(result.Errors);
            }
            finally
            {
                File.Delete(xsdPath);
            }
        }

        [Fact]
        public void ValidateContent_ShouldPassForValidXml()
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

            string xml = "<root><child>123</child></root>";

            string xsdPath = Path.GetTempFileName();
            File.WriteAllText(xsdPath, xsd);

            try
            {
                var validator = new XmlSchemaValidator(new List<string> { xsdPath });
                var result = validator.ValidateContent(xml);

                Assert.True(result.IsValid);
                Assert.Empty(result.Errors);
            }
            finally
            {
                File.Delete(xsdPath);
            }
        }

        [Fact]
        public void ValidateContent_ShouldUseEmbeddedSchemaSet()
        {
            string xml = "<root><child>123</child></root>";
            var assembly = typeof(XmlSchemaValidatorTests).Assembly;
            string resourceName = "XmlComparer.Tests.Resources.SampleSchema.xsd";

            var resources = XmlSchemaSetFactory.ListEmbeddedSchemas(assembly);
            Assert.Contains(resourceName, resources);

            var schemaSet = XmlSchemaSetFactory.FromEmbeddedResource(assembly, resourceName);
            var validator = new XmlSchemaValidator(schemaSet);
            var result = validator.ValidateContent(xml);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validator_ShouldThrowOnInvalidSchema()
        {
            string invalidXsd = "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='root'></xs:schema>";
            string xsdPath = Path.GetTempFileName();
            File.WriteAllText(xsdPath, invalidXsd);

            try
            {
                var ex = Assert.ThrowsAny<Exception>(() => new XmlSchemaValidator(new List<string> { xsdPath }));
                Assert.True(ex is XmlException || ex is InvalidOperationException);
            }
            finally
            {
                File.Delete(xsdPath);
            }
        }

        [Fact]
        public void CompareFilesWithReport_ShouldProduceHtmlAndJson()
        {
            string xml1 = "<root><child>1</child></root>";
            string xml2 = "<root><child>2</child></root>";

            string file1 = Path.GetTempFileName();
            string file2 = Path.GetTempFileName();
            File.WriteAllText(file1, xml1);
            File.WriteAllText(file2, xml2);

            try
            {
                var result = XmlComparer.Core.XmlComparer.CompareFilesWithReport(
                    file1,
                    file2,
                    options => options.IncludeHtml().IncludeJson());

                Assert.NotNull(result.Diff);
                Assert.NotNull(result.Html);
                Assert.NotNull(result.Json);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }
    }
}
