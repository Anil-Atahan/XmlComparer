using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XmlComparer.Core;

namespace XmlComparer.Runner
{
    /// <summary>
    /// Processes batch XML comparisons on multiple file pairs.
    /// </summary>
    /// <remarks>
    /// <para>The batch processor enables comparison of multiple file pairs in a single run.
    /// File pairs can be specified in a batch file or discovered via directory patterns.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var processor = new BatchProcessor();
    ///
    /// // Process a batch file
    /// var results = processor.ProcessBatch("pairs.batch");
    ///
    /// // Process directory pairs
    /// var results = processor.ProcessDirectories("original/", "modified/", "*.xml");
    ///
    /// // Process with parallel execution
    /// var options = new BatchOptions { MaxParallelism = 4 };
    /// var results = processor.ProcessBatch("pairs.batch", options);
    /// </code>
    /// </example>
    public class BatchProcessor
    {
        /// <summary>
        /// Creates a new BatchProcessor.
        /// </summary>
        public BatchProcessor()
        {
        }

        /// <summary>
        /// Processes a batch file containing file pairs.
        /// </summary>
        /// <param name="batchFilePath">Path to the batch file.</param>
        /// <param name="options">Batch processing options.</param>
        /// <returns>A batch result with all comparison results.</returns>
        public BatchResult ProcessBatch(string batchFilePath, BatchOptions? options = null)
        {
            options ??= new BatchOptions();

            var pairs = ParseBatchFile(batchFilePath);
            return ProcessPairs(pairs, options);
        }

        /// <summary>
        /// Processes all matching file pairs in two directories.
        /// </summary>
        /// <param name="originalDir">Directory containing original files.</param>
        /// <param name="newDir">Directory containing new files.</param>
        /// <param name="searchPattern">File pattern (e.g., "*.xml").</param>
        /// <param name="options">Batch processing options.</param>
        /// <returns>A batch result with all comparison results.</returns>
        public BatchResult ProcessDirectories(
            string originalDir,
            string newDir,
            string searchPattern = "*.xml",
            BatchOptions? options = null)
        {
            options ??= new BatchOptions();

            var originalFiles = Directory.GetFiles(originalDir, searchPattern, SearchOption.AllDirectories);
            var newFiles = Directory.GetFiles(newDir, searchPattern, SearchOption.AllDirectories);

            var pairs = new List<FilePair>();

            foreach (var origFile in originalFiles)
            {
                string relativePath = Path.GetRelativePath(originalDir, origFile);
                string newFile = Path.Combine(newDir, relativePath);

                if (File.Exists(newFile))
                {
                    pairs.Add(new FilePair
                    {
                        OriginalPath = origFile,
                        NewPath = newFile,
                        Name = Path.GetFileNameWithoutExtension(origFile)
                    });
                }
            }

            return ProcessPairs(pairs, options);
        }

        /// <summary>
        /// Processes a list of file pairs.
        /// </summary>
        public BatchResult ProcessPairs(List<FilePair> pairs, BatchOptions options)
        {
            var result = new BatchResult
            {
                StartTime = DateTime.UtcNow,
                TotalPairs = pairs.Count
            };

            var stopwatch = Stopwatch.StartNew();

            if (options.MaxParallelism > 1)
            {
                // Parallel processing
                var tasks = pairs.Select(pair => Task.Run(() =>
                    ProcessSinglePair(pair, options, result)));

                Task.WhenAll(tasks).Wait();
            }
            else
            {
                // Sequential processing
                foreach (var pair in pairs)
                {
                    ProcessSinglePair(pair, options, result);
                }
            }

            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            return result;
        }

        /// <summary>
        /// Processes a single file pair.
        /// </summary>
        private SingleResult ProcessSinglePair(FilePair pair, BatchOptions options, BatchResult batchResult)
        {
            var singleResult = new SingleResult
            {
                PairName = pair.Name,
                OriginalPath = pair.OriginalPath,
                NewPath = pair.NewPath
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Load files
                var originalXml = File.ReadAllText(pair.OriginalPath);
                var newXml = File.ReadAllText(pair.NewPath);

                // Compare
                var config = options.Config?.ToComparisonOptions() ?? new XmlComparisonOptions();
                var comparer = new XmlComparerService(config.BuildConfig());
                var diff = comparer.CompareXml(originalXml, newXml);

                singleResult.Diff = diff;
                singleResult.Success = true;
                singleResult.TotalChanges = CountChanges(diff);

                // Save output if specified
                if (!string.IsNullOrEmpty(options.OutputDirectory))
                {
                    SaveOutput(pair, diff, options, singleResult);
                }
            }
            catch (Exception ex)
            {
                singleResult.Success = false;
                singleResult.ErrorMessage = ex.Message;
                batchResult.FailedCount++;
            }

            stopwatch.Stop();
            singleResult.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            lock (batchResult.Results)
            {
                batchResult.Results.Add(singleResult);
                batchResult.CompletedCount++;

                if (options.ProgressCallback != null)
                {
                    options.ProgressCallback(batchResult);
                }
            }

            return singleResult;
        }

        /// <summary>
        /// Counts total changes in a diff.
        /// </summary>
        private int CountChanges(DiffMatch diff)
        {
            int count = diff.Type != DiffType.Unchanged ? 1 : 0;

            foreach (var child in diff.Children)
            {
                count += CountChanges(child);
            }

            return count;
        }

        /// <summary>
        /// Saves output for a single comparison.
        /// </summary>
        private void SaveOutput(FilePair pair, DiffMatch diff, BatchOptions options, SingleResult result)
        {
            string outputDir = options.OutputDirectory!;
            Directory.CreateDirectory(outputDir);

            string baseName = pair.Name ?? "output";
            string outputPath = Path.Combine(outputDir, $"{baseName}.{options.OutputExtension}");

            switch (options.OutputFormat?.ToLowerInvariant())
            {
                case "json":
                    var formatter = new JsonDiffFormatter();
                    File.WriteAllText(outputPath, formatter.Format(diff));
                    result.OutputPath = outputPath;
                    break;

                case "html":
                    var htmlFormatter = new HtmlDiffFormatter();
                    File.WriteAllText(outputPath, htmlFormatter.Format(diff));
                    result.OutputPath = outputPath;
                    break;

                default:
                    var textFormatter = new TextDiffFormatter();
                    File.WriteAllText(outputPath, textFormatter.Format(diff));
                    result.OutputPath = outputPath;
                    break;
            }
        }

        /// <summary>
        /// Parses a batch file containing file pairs.
        /// </summary>
        private List<FilePair> ParseBatchFile(string batchFilePath)
        {
            var pairs = new List<FilePair>();

            foreach (var line in File.ReadAllLines(batchFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    pairs.Add(new FilePair
                    {
                        OriginalPath = parts[0],
                        NewPath = parts[1],
                        Name = parts.Length > 2 ? parts[2] : Path.GetFileNameWithoutExtension(parts[0])
                    });
                }
            }

            return pairs;
        }
    }

    /// <summary>
    /// Represents a pair of files to compare.
    /// </summary>
    public class FilePair
    {
        /// <summary>
        /// Gets or sets the path to the original file.
        /// </summary>
        public string OriginalPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the new file.
        /// </summary>
        public string NewPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name for this pair.
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Options for batch processing.
    /// </summary>
    public class BatchOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of parallel comparisons.
        /// </summary>
        public int MaxParallelism { get; set; } = 1;

        /// <summary>
        /// Gets or sets the output directory for results.
        /// </summary>
        public string? OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        public string? OutputFormat { get; set; }

        /// <summary>
        /// Gets or sets the output file extension.
        /// </summary>
        public string OutputExtension { get; set; } = "html";

        /// <summary>
        /// Gets or sets the comparison configuration.
        /// </summary>
        public ComparisonConfiguration? Config { get; set; }

        /// <summary>
        /// Gets or sets the progress callback.
        /// </summary>
        public Action<BatchResult>? ProgressCallback { get; set; }

        /// <summary>
        /// Gets or sets whether to stop on first error.
        /// </summary>
        public bool StopOnError { get; set; } = false;
    }

    /// <summary>
    /// Result of a batch processing operation.
    /// </summary>
    public class BatchResult
    {
        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the elapsed milliseconds.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets the total number of pairs.
        /// </summary>
        public int TotalPairs { get; set; }

        /// <summary>
        /// Gets or sets the number of completed pairs.
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of failed pairs.
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Gets or sets the individual results.
        /// </summary>
        public List<SingleResult> Results { get; set; } = new List<SingleResult>();

        /// <summary>
        /// Gets whether all comparisons succeeded.
        /// </summary>
        public bool AllSucceeded => FailedCount == 0;

        /// <summary>
        /// Gets the number of pairs with differences.
        /// </summary>
        public int PairsWithDifferences => Results.Count(r => r.TotalChanges > 0);
    }

    /// <summary>
    /// Result of a single comparison in a batch.
    /// </summary>
    public class SingleResult
    {
        /// <summary>
        /// Gets or sets the pair name.
        /// </summary>
        public string? PairName { get; set; }

        /// <summary>
        /// Gets or sets the original file path.
        /// </summary>
        public string? OriginalPath { get; set; }

        /// <summary>
        /// Gets or sets the new file path.
        /// </summary>
        public string? NewPath { get; set; }

        /// <summary>
        /// Gets or sets whether the comparison succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the diff result.
        /// </summary>
        public DiffMatch? Diff { get; set; }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the total number of changes.
        /// </summary>
        public int TotalChanges { get; set; }

        /// <summary>
        /// Gets or sets the elapsed milliseconds.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }
}
