using System.Net;
using System.Xml.Linq;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class HtmlSideBySideFormatterTests
    {
        [Fact]
        public void GenerateHtml_ShouldEncodeAttributeValues()
        {
            string rawValue = "x\" <bad> & more";
            string encodedValue = WebUtility.HtmlEncode(rawValue);

            var original = new XElement("root", new XAttribute("data", rawValue));
            var updated = new XElement("root", new XAttribute("data", rawValue));

            var diff = new DiffMatch
            {
                OriginalElement = original,
                NewElement = updated,
                Type = DiffType.Unchanged
            };

            var formatter = new HtmlSideBySideFormatter();
            string html = formatter.GenerateHtml(diff);

            Assert.Contains(encodedValue, html);
            Assert.DoesNotContain(rawValue, html);
        }

        [Fact]
        public void GenerateHtml_ShouldEmbedJsonWhenProvided()
        {
            string json = "{\"a\":1}";

            var original = new XElement("root");
            var updated = new XElement("root");

            var diff = new DiffMatch
            {
                OriginalElement = original,
                NewElement = updated,
                Type = DiffType.Unchanged
            };

            var formatter = new HtmlSideBySideFormatter();
            string html = formatter.GenerateHtml(diff, json);

            Assert.Contains("id=\"xml-diff-json\"", html);
            Assert.Contains(json, html);
            Assert.Contains("id=\"validation-panel\"", html);
        }

        [Fact]
        public void GenerateHtml_ShouldEscapeScriptEndSequenceInJson()
        {
            string json = "{\"x\":\"</script>\"}";

            var original = new XElement("root");
            var updated = new XElement("root");

            var diff = new DiffMatch
            {
                OriginalElement = original,
                NewElement = updated,
                Type = DiffType.Unchanged
            };

            var formatter = new HtmlSideBySideFormatter();
            string html = formatter.GenerateHtml(diff, json);

            Assert.Contains("<\\/script>", html);
            Assert.DoesNotContain("</script>\"", html);
        }

        [Fact]
        public void GenerateHtml_ShouldHighlightChangedAttributes()
        {
            var original = new XElement("root", new XAttribute("status", "old"));
            var updated = new XElement("root", new XAttribute("status", "new"));

            var diff = new DiffMatch
            {
                OriginalElement = original,
                NewElement = updated,
                Type = DiffType.Modified
            };

            var formatter = new HtmlSideBySideFormatter();
            string html = formatter.GenerateHtml(diff);

            Assert.Contains("attr-changed", html);
        }
    }
}
