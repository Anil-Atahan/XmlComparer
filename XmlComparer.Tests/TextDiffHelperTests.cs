using System.Collections.Generic;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class TextDiffHelperTests
    {
        [Fact]
        public void Tokenize_ShouldSplitByWhitespaceAndPunctuation()
        {
            string text = "Hello, world!";
            var tokens = TextDiffHelper.Tokenize(text);

            Assert.Contains("Hello", tokens);
            Assert.Contains(",", tokens);
            Assert.Contains("world", tokens);
            Assert.Contains("!", tokens);
        }

        [Fact]
        public void GetDiffs_ShouldIdentifyChanges()
        {
            string oldText = "Hello world";
            string newText = "Hello C# world";

            var diffs = TextDiffHelper.GetDiffs(oldText, newText);

            // Expected: Hello (Unchanged), C# (Added), world (Unchanged)
            // Note: Spaces are part of tokenization depending on regex, let's check types
            
            Assert.Contains(diffs, d => d.Token == "Hello" && d.Type == DiffType.Unchanged);
            Assert.Contains(diffs, d => d.Token.Contains("C#") && d.Type == DiffType.Added);
            Assert.Contains(diffs, d => d.Token == "world" && d.Type == DiffType.Unchanged);
        }
    }
}
