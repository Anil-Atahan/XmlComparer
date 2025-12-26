using System.Xml.Linq;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class MatchingStrategyTests
    {
        [Fact]
        public void Score_ShouldReturnHighForSameNameAndKeyAttribute()
        {
            var config = new XmlDiffConfig();
            config.KeyAttributeNames.Add("id");

            var e1 = new XElement("item", new XAttribute("id", "1"), "Value");
            var e2 = new XElement("item", new XAttribute("id", "1"), "Value Modified");

            var strategy = new DefaultMatchingStrategy();
            double score = strategy.Score(e1, e2, config);

            Assert.True(score > 10.0, "Score should be high due to key match");
        }

        [Fact]
        public void Score_ShouldReturnZeroForDifferentNames()
        {
            var config = new XmlDiffConfig();
            var e1 = new XElement("item");
            var e2 = new XElement("other");

            var strategy = new DefaultMatchingStrategy();
            double score = strategy.Score(e1, e2, config);

            Assert.Equal(0.0, score);
        }
    }
}
