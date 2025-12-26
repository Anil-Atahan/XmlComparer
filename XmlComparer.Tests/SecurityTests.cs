using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Schema;
using Xunit;
using XmlComparer.Core;
using CoreXmlSchemaValidator = XmlComparer.Core.XmlSchemaValidator;

namespace XmlComparer.Tests
{
    /// <summary>
    /// Security tests to verify protection against XXE attacks, path traversal, and DoS attempts.
    /// </summary>
    public class SecurityTests
    {
        [Fact]
        public void CompareContent_ShouldHandleXXEAttempt_Simple()
        {
            // This XML contains a DTD with an entity reference
            string xxeXml = @"<?xml version=""1.0""?>
<!DOCTYPE root [
    <!ENTITY test ""Hello"">
]>
<root>&test;</root>";

            var service = new XmlComparerService(new XmlDiffConfig());

            // Should handle securely - DTD processing is disabled
            var exception = Record.Exception(() => service.CompareXml(xxeXml, xxeXml));

            // The secure implementation should either throw or handle safely
            // With DtdProcessing.Prohibit, this should work (entity not resolved)
            Assert.NotNull(exception);
        }

        [Fact]
        public void CompareFiles_ShouldRejectRelativePathTraversal()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            // Path traversal should be rejected
            var exception = Assert.Throws<ArgumentException>(() =>
                service.Compare("../../../etc/passwd", "test.xml"));

            Assert.Contains("traversal", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CompareFiles_ShouldRejectTildePathTraversal()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            // Tilde paths can be used for path traversal on Windows
            var exception = Assert.Throws<ArgumentException>(() =>
                service.Compare("~/../../test.xml", "test.xml"));

            Assert.Contains("traversal", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CompareAsync_ShouldHandleExternalDtd()
        {
            // External DTD reference
            string externalDtdXml = @"<?xml version=""1.0""?>
<!DOCTYPE root SYSTEM ""http://example.com/evil.dtd"">
<root>test</root>";

            var service = new XmlComparerService(new XmlDiffConfig());

            // Should not fetch external resources due to XmlResolver = null
            var exception = await Record.ExceptionAsync(async () =>
                await service.CompareXmlAsync(externalDtdXml, externalDtdXml));

            // Should fail securely without making external requests
            Assert.NotNull(exception);
        }

        [Fact]
        public void ValidateWithXsds_ShouldHandleMaliciousSchema()
        {
            // Schema with external entity reference
            string maliciousXsd = @"<?xml version=""1.0""?>
<!DOCTYPE xsd [
    <!ENTITY xxe SYSTEM ""file:///etc/passwd"">
]>
<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <xs:element name='root' type='xs:string'/>
</xs:schema>";

            string xsdPath = Path.GetTempFileName();
            File.WriteAllText(xsdPath, maliciousXsd);

            try
            {
                var exception = Assert.ThrowsAny<Exception>(() =>
                    new CoreXmlSchemaValidator(new[] { xsdPath }));

                // Should either throw or safely ignore external entities
                Assert.NotNull(exception);
            }
            finally
            {
                File.Delete(xsdPath);
            }
        }

        [Fact]
        public void CompareFiles_ShouldRejectInvalidCharactersInPath()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            // Paths with null characters are invalid
            var exception = Assert.Throws<ArgumentException>(() =>
                service.Compare("test\x00.xml", "test.xml"));

            Assert.Contains("invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CompareXml_ShouldRejectEmptyContent()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            var exception = Assert.Throws<ArgumentException>(() =>
                service.CompareXml("", ""));

            Assert.Contains("null or empty", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CompareXml_ShouldRejectNullContent()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.Throws<ArgumentException>(() =>
                service.CompareXml(null!, "test"));
        }

        [Fact]
        public void CompareXml_ShouldRejectWhitespaceOnlyContent()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            var exception = Assert.Throws<ArgumentException>(() =>
                service.CompareXml("   \n\t  ", "   "));

            Assert.Contains("null or empty", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateContent_ShouldRejectEmptyContent()
        {
            var validator = new CoreXmlSchemaValidator(new XmlSchemaSet());

            var exception = Assert.Throws<ArgumentException>(() =>
                validator.ValidateContent(""));

            Assert.Contains("null or empty", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void XmlSchemaSetFactory_FromFiles_ShouldValidatePaths()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                XmlSchemaSetFactory.FromFiles(new[] { "../../../test.xsd" }));

            Assert.Contains("traversal", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void XmlSchemaSetFactory_FromEmbeddedResources_ShouldThrowOnNullAssembly()
        {
            Assert.Throws<ArgumentNullException>(() =>
                XmlSchemaSetFactory.FromEmbeddedResources(null!, new[] { "test" }));
        }

        [Fact]
        public void XmlSchemaSetFactory_FromEmbeddedResources_ShouldThrowOnNullResourceNames()
        {
            var assembly = typeof(XmlSchemaSetFactory).Assembly;

            Assert.Throws<ArgumentNullException>(() =>
                XmlSchemaSetFactory.FromEmbeddedResources(assembly, null!));
        }

        [Fact]
        public void XmlSchemaSetFactory_ListEmbeddedSchemas_ShouldThrowOnNullAssembly()
        {
            Assert.Throws<ArgumentNullException>(() =>
                XmlSchemaSetFactory.ListEmbeddedSchemas(null!));
        }
    }
}
