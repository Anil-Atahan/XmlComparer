using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    /// <summary>
    /// Tests for error handling and edge case scenarios.
    /// </summary>
    public class ErrorHandlingTests
    {
        [Fact]
        public void CompareContent_ShouldHandleMalformedXml()
        {
            string malformed = "<root><child></root>"; // Mismatched tags

            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.ThrowsAny<Exception>(() =>
                service.CompareXml(malformed, "<root></root>"));
        }

        [Fact]
        public void CompareFiles_ShouldThrowOnFileNotFound()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.Throws<FileNotFoundException>(() =>
                service.Compare("nonexistent-file-12345.xml", "nonexistent-file-67890.xml"));
        }

        [Fact]
        public async Task CompareAsync_ShouldThrowOnFileNotFound()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await service.CompareAsync("nonexistent-file-12345.xml", "nonexistent-file-67890.xml"));
        }

        [Fact]
        public void CompareContent_ShouldHandleInvalidCharacters()
        {
            string invalidXml = "<root>\x00\x01\x02</root>"; // Invalid XML chars

            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.ThrowsAny<Exception>(() =>
                service.CompareXml(invalidXml, invalidXml));
        }

        [Fact]
        public void ValidateWithXsds_ShouldThrowOnInvalidSchemaPath()
        {
            Assert.ThrowsAny<Exception>(() =>
                new XmlSchemaValidator(new[] { "nonexistent-schema-12345.xsd" }));
        }

        [Fact]
        public void ValidateWithXsds_ShouldThrowOnMalformedSchema()
        {
            string malformedXsd = "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='root'></xs:schema>";
            string xsdPath = Path.GetTempFileName();
            File.WriteAllText(xsdPath, malformedXsd);

            try
            {
                Assert.ThrowsAny<Exception>(() =>
                    new XmlSchemaValidator(new[] { xsdPath }));
            }
            finally
            {
                File.Delete(xsdPath);
            }
        }

        [Fact]
        public void Options_FromJson_ShouldThrowOnInvalidJson()
        {
            Assert.ThrowsAny<Exception>(() =>
                XmlComparisonOptions.FromJson("not valid json"));
        }

        [Fact]
        public void Options_FromJson_ShouldThrowOnEmptyJson()
        {
            Assert.ThrowsAny<Exception>(() =>
                XmlComparisonOptions.FromJson(""));
        }

        [Fact]
        public void Options_LoadFromFile_ShouldThrowOnNonexistentFile()
        {
            Assert.Throws<FileNotFoundException>(() =>
                XmlComparisonOptions.LoadFromFile("nonexistent-options-12345.json"));
        }

        [Fact]
        public void Options_LoadFromFile_ShouldThrowOnInvalidJson()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "invalid json content");

            try
            {
                Assert.ThrowsAny<Exception>(() =>
                    XmlComparisonOptions.LoadFromFile(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void CompareFiles_ShouldThrowOnNullConfig()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new XmlComparerService(null!));
        }

        [Fact]
        public void CompareStreams_ShouldThrowOnNullStream()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.Throws<ArgumentNullException>(() =>
                service.Compare(null!, new MemoryStream()));
        }

        [Fact]
        public void CompareStreams_ShouldThrowOnBothNullStreams()
        {
            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.Throws<ArgumentException>(() =>
                service.Compare((string)null!, null!));
        }

        [Fact]
        public void XmlSchemaValidator_Constructor_ShouldThrowOnNullPaths()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new XmlSchemaValidator((IEnumerable<string>)null!));
        }

        [Fact]
        public void XmlSchemaValidator_ConstructorWithSchemaSet_ShouldThrowOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new XmlSchemaValidator((System.Xml.Schema.XmlSchemaSet)null!));
        }

        [Fact]
        public void XmlComparerService_Constructor_ShouldThrowOnNullConfig()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new XmlComparerService(null!));
        }

        [Fact]
        public void CompareContent_ShouldHandleUnclosedComment()
        {
            string unclosedComment = "<root><!-- unclosed comment</root>";

            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.ThrowsAny<Exception>(() =>
                service.CompareXml(unclosedComment, unclosedComment));
        }

        [Fact]
        public void CompareContent_ShouldHandleMismatchedQuotes()
        {
            string mismatchedQuotes = "<root attr=\"value'></root>";

            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.ThrowsAny<Exception>(() =>
                service.CompareXml(mismatchedQuotes, mismatchedQuotes));
        }

        [Fact]
        public void CompareContent_ShouldHandleDuplicateAttributes()
        {
            // This is actually invalid XML that the parser should catch
            string duplicateAttrs = "<root attr='1' attr='2'/>";

            var service = new XmlComparerService(new XmlDiffConfig());

            Assert.ThrowsAny<Exception>(() =>
                service.CompareXml(duplicateAttrs, duplicateAttrs));
        }

        [Fact]
        public void ValidateContent_ShouldThrowOnEmptyContent()
        {
            var validator = new XmlSchemaValidator(new System.Xml.Schema.XmlSchemaSet());

            Assert.Throws<ArgumentException>(() =>
                validator.ValidateContent(""));
        }

        [Fact]
        public void ValidateContent_ShouldThrowOnNullContent()
        {
            var validator = new XmlSchemaValidator(new System.Xml.Schema.XmlSchemaSet());

            Assert.Throws<ArgumentException>(() =>
                validator.ValidateContent(null!));
        }

        [Fact]
        public async Task ValidateContentAsync_ShouldThrowOnEmptyContent()
        {
            var validator = new XmlSchemaValidator(new System.Xml.Schema.XmlSchemaSet());

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await validator.ValidateContentAsync(""));
        }

        [Fact]
        public void CompareFiles_ShouldHandleLargeFileWithinLimits()
        {
            // Create a file under the 100MB limit
            string tempFile1 = Path.GetTempFileName();
            string tempFile2 = Path.GetTempFileName();

            try
            {
                string xml = "<root><item>test</item></root>";
                File.WriteAllText(tempFile1, xml);
                File.WriteAllText(tempFile2, xml);

                var service = new XmlComparerService(new XmlDiffConfig());
                var diff = service.Compare(tempFile1, tempFile2);

                Assert.NotNull(diff);
                Assert.Equal(DiffType.Unchanged, diff.Type);
            }
            finally
            {
                File.Delete(tempFile1);
                File.Delete(tempFile2);
            }
        }
    }
}
