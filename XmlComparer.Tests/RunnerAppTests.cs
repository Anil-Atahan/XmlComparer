using Xunit;
using XmlComparer.Runner;

namespace XmlComparer.Tests
{
    public class RunnerAppTests
    {
        [Fact]
        public void ParseArgs_ShouldHandleCommonOptions()
        {
            string[] args =
            {
                "a.xml",
                "b.xml",
                "--out",
                "report.html",
                "--json",
                "report.json",
                "--json-only",
                "--validation-only",
                "--ignore-values",
                "--key",
                "id,code",
                "--xsd",
                "a.xsd",
                "--xsd",
                "b.xsd"
            };

            var options = RunnerApp.ParseArgs(args);

            Assert.Equal(2, options.Positionals.Count);
            Assert.Equal("report.html", options.OutputPath);
            Assert.Equal("report.json", options.JsonOut);
            Assert.True(options.JsonOnly);
            Assert.True(options.ValidationOnly);
            Assert.True(options.IgnoreValues);
            Assert.Contains("id", options.KeyAttributes);
            Assert.Contains("code", options.KeyAttributes);
            Assert.Contains("a.xsd", options.XsdPaths);
            Assert.Contains("b.xsd", options.XsdPaths);
        }

        [Fact]
        public void ParseArgs_ShouldHandleHelp()
        {
            var options = RunnerApp.ParseArgs(new[] { "--help" });
            Assert.True(options.ShowHelp);
        }

        [Fact]
        public void ParseArgs_ShouldDefaultJsonNameWhenFlagHasNoValue()
        {
            var options = RunnerApp.ParseArgs(new[] { "a.xml", "b.xml", "--json" });
            Assert.Equal("diff.json", options.JsonOut);
        }

        [Fact]
        public void ParseArgs_ShouldTreatUnknownFlagsAsPositionals()
        {
            var options = RunnerApp.ParseArgs(new[] { "a.xml", "b.xml", "--unknown" });
            Assert.Contains("Unknown option: --unknown", options.Errors);
        }

        [Fact]
        public void ParseArgs_ShouldIgnoreIncompleteOutFlag()
        {
            var options = RunnerApp.ParseArgs(new[] { "a.xml", "b.xml", "--out" });
            Assert.Contains("Missing value for --out.", options.Errors);
            Assert.Null(options.OutputPath);
        }

        [Fact]
        public void ParseArgs_ShouldReportMissingKeyValue()
        {
            var options = RunnerApp.ParseArgs(new[] { "a.xml", "b.xml", "--key" });
            Assert.Contains("Missing value for --key.", options.Errors);
        }

        [Fact]
        public void ParseArgs_ShouldReportMissingXsdValue()
        {
            var options = RunnerApp.ParseArgs(new[] { "a.xml", "b.xml", "--xsd" });
            Assert.Contains("Missing value for --xsd.", options.Errors);
        }
    }
}
