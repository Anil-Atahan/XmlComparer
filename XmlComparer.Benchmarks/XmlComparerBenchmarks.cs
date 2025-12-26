using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Xml.Linq;
using XmlComparer.Core;

namespace XmlComparer.Benchmarks
{
    using static XmlComparer.Core.XmlComparer;

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    [Config(typeof(BenchmarkConfig))]
    public class XmlComparerBenchmarks
    {
        private class BenchmarkConfig : ManualConfig
        {
            public BenchmarkConfig()
            {
                AddDiagnoser(MemoryDiagnoser.Default);
                AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()));
            }
        }

        // Test data
        private string _smallXml1 = null!;
        private string _smallXml2 = null!;
        private string _mediumXml1 = null!;
        private string _mediumXml2 = null!;
        private XDocument _smallDoc1 = null!;
        private XDocument _smallDoc2 = null!;
        private XDocument _mediumDoc1 = null!;
        private XDocument _mediumDoc2 = null!;

        private XmlComparerService _service = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Generate test data
            _smallXml1 = GenerateXml(100, 5, false);
            _smallXml2 = GenerateXml(100, 5, true);
            _mediumXml1 = GenerateXml(5000, 10, false);
            _mediumXml2 = GenerateXml(5000, 10, true);

            _smallDoc1 = XDocument.Parse(_smallXml1);
            _smallDoc2 = XDocument.Parse(_smallXml2);
            _mediumDoc1 = XDocument.Parse(_mediumXml1);
            _mediumDoc2 = XDocument.Parse(_mediumXml2);

            var config = new XmlDiffConfig
            {
                KeyAttributeNames = new HashSet<string> { "id" },
                ExcludedAttributeNames = new HashSet<string> { "timestamp" },
                NormalizeWhitespace = true,
                TrimValues = true
            };

            _service = new XmlComparerService(config);
        }

        #region Core Comparison Benchmarks

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(5000)]
        public DiffMatch CompareXmlContent(int nodeCount)
        {
            var xml1 = GenerateXml(nodeCount, 10, false);
            var xml2 = GenerateXml(nodeCount, 10, true);
            return CompareContent(xml1, xml2);
        }

        [Benchmark]
        public DiffMatch CompareSmallDocuments()
        {
            return CompareContent(_smallXml1, _smallXml2);
        }

        [Benchmark]
        public DiffMatch CompareMediumDocuments()
        {
            return CompareContent(_mediumXml1, _mediumXml2);
        }

        #endregion

        #region LCS Algorithm Benchmarks

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public List<int> FindLcs(int sequenceLength)
        {
            var seq1 = Enumerable.Range(0, sequenceLength).ToList();
            var seq2 = Enumerable.Range(sequenceLength / 2, sequenceLength).ToList();
            return LcsHelper.FindLcs(seq1, seq2);
        }

        [Benchmark]
        public List<string> FindLcsStringList()
        {
            var seq1 = Enumerable.Range(0, 1000).Select(i => $"item{i}").ToList();
            var seq2 = Enumerable.Range(500, 1000).Select(i => $"item{i}").ToList();
            return LcsHelper.FindLcs(seq1, seq2, (a, b) => a == b);
        }

        #endregion

        #region HTML Generation Benchmarks

        [Benchmark]
        public string GenerateHtml_SmallDiff()
        {
            var diff = CompareContent(_smallXml1, _smallXml2);
            return _service.GenerateHtml(diff);
        }

        [Benchmark]
        public string GenerateHtml_MediumDiff()
        {
            var diff = CompareContent(_mediumXml1, _mediumXml2);
            return _service.GenerateHtml(diff);
        }

        [Benchmark]
        public string GenerateHtml_WithJson()
        {
            var diff = CompareContent(_smallXml1, _smallXml2);
            return _service.GenerateHtml(diff, true);
        }

        #endregion

        #region JSON Generation Benchmarks

        [Benchmark]
        public string GenerateJson_SmallDiff()
        {
            var diff = CompareContent(_smallXml1, _smallXml2);
            return _service.GenerateJson(diff);
        }

        [Benchmark]
        public string GenerateJson_MediumDiff()
        {
            var diff = CompareContent(_mediumXml1, _mediumXml2);
            return _service.GenerateJson(diff);
        }

        #endregion

        #region Summary Calculation Benchmarks

        [Benchmark]
        public DiffSummary CalculateSummary_Small()
        {
            var diff = CompareContent(_smallXml1, _smallXml2);
            return _service.GetSummary(diff);
        }

        [Benchmark]
        public DiffSummary CalculateSummary_Medium()
        {
            var diff = CompareContent(_mediumXml1, _mediumXml2);
            return _service.GetSummary(diff);
        }

        #endregion

        #region Helper Methods

        private string GenerateXml(int nodeCount, int attributeCount, bool makeDifferent)
        {
            var random = new Random(42); // Fixed seed for reproducibility
            var root = new XElement("root");

            for (int i = 0; i < nodeCount; i++)
            {
                var element = new XElement($"item{i}");

                // Add attributes
                for (int j = 0; j < attributeCount; j++)
                {
                    var attrName = $"attr{j}";
                    var attrValue = makeDifferent && random.Next(100) < 10
                        ? $"modified{i}_{j}"
                        : $"value{i}_{j}";
                    element.Add(new XAttribute(attrName, attrValue));
                }

                // Add key attribute
                element.Add(new XAttribute("id", $"item{i}"));

                // Add some child elements
                if (i % 10 == 0)
                {
                    element.Add(new XElement("child", $"content{i}"));
                }

                root.Add(element);
            }

            // Add some elements that will be deleted/added
            if (makeDifferent)
            {
                root.Add(new XElement("deletedNode", new XAttribute("id", "del1")));
            }

            return root.ToString();
        }

        #endregion
    }

    #region Specialized Benchmarks for Hot Paths

    [MemoryDiagnoser]
    public class PathBuildingBenchmarks
    {
        private XElement _deepElement = null!;
        private XElement _wideElement = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Create deep hierarchy
            var current = new XElement("level100");
            for (int i = 99; i >= 0; i--)
            {
                var parent = new XElement($"level{i}");
                parent.Add(current);
                current = parent;
            }
            _deepElement = current;

            // Create wide hierarchy (element at position 500 among siblings)
            var root = new XElement("root",
                Enumerable.Range(0, 1000)
                    .Select(i => new XElement($"child{i}", new XAttribute("id", $"child{i}"))));

            _wideElement = root.Descendants().Last();
        }

        [Benchmark]
        public string BuildPath_DeepElement()
        {
            return BuildPath(_deepElement);
        }

        [Benchmark]
        public string BuildPath_WideElement()
        {
            return BuildPath(_wideElement);
        }

        private string BuildPath(XElement element)
        {
            var parts = new Stack<string>();
            var current = element;

            while (current != null)
            {
                int index = 1;
                if (current.Parent != null)
                {
                    index = current.Parent.Elements(current.Name)
                        .TakeWhile(e => e != current)
                        .Count() + 1;
                }
                parts.Push($"{current.Name.LocalName}[{index}]");
                current = current.Parent;
            }

            return "/" + string.Join("/", parts);
        }
    }

    #endregion

    #region Program Entry Point

    public class Program
    {
        public static void Main(string[] args)
        {
            // Run all benchmarks
            BenchmarkRunner.Run<XmlComparerBenchmarks>();
            BenchmarkRunner.Run<PathBuildingBenchmarks>();
        }
    }

    #endregion
}
