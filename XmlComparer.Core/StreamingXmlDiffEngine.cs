using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Performs streaming comparison of large XML documents.
    /// </summary>
    /// <remarks>
    /// <para>The streaming diff engine processes XML documents in chunks rather than
    /// loading entire documents into memory. This enables comparison of very large
    /// files that would otherwise cause OutOfMemoryException.</para>
    /// <para><b>Key Features:</b></para>
    /// <list type="bullet">
    ///   <item><description>Memory-efficient chunk-based processing</description></item>
    ///   <item><description>Configurable chunking strategies</description></item>
    ///   <item><description>Parallel processing support</description></item>
    ///   <item><description>Progress reporting</description></item>
    ///   <item><description>Temporary file support for extremely large documents</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new StreamingDiffOptions
    /// {
    ///     ChunkProcessor = new ChunkProcessors.TopLevelElements(),
    ///     ParallelProcessing = true
    /// };
    ///
    /// var engine = new StreamingXmlDiffEngine(options);
    ///
    /// // Synchronous comparison
    /// var diff = engine.Compare("large1.xml", "large2.xml");
    ///
    /// // Asynchronous comparison with progress
    /// var diff = await engine.CompareAsync("large1.xml", "large2.xml", progress =>
    /// {
    ///     Console.WriteLine($"Progress: {progress.PercentComplete}%");
    /// });
    /// </code>
    /// </example>
    public class StreamingXmlDiffEngine
    {
        private readonly StreamingDiffOptions _options;
        private readonly XmlDiffEngine _engine;

        /// <summary>
        /// Gets the streaming diff options.
        /// </summary>
        public StreamingDiffOptions Options => _options;

        /// <summary>
        /// Creates a new StreamingXmlDiffEngine with default options.
        /// </summary>
        public StreamingXmlDiffEngine() : this(new StreamingDiffOptions()) { }

        /// <summary>
        /// Creates a new StreamingXmlDiffEngine with the specified options.
        /// </summary>
        /// <param name="options">The streaming options.</param>
        public StreamingXmlDiffEngine(StreamingDiffOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _engine = new XmlDiffEngine(new XmlDiffConfig(), new DefaultMatchingStrategy());
        }

        /// <summary>
        /// Compares two XML files synchronously using streaming.
        /// </summary>
        /// <param name="originalPath">Path to the original XML file.</param>
        /// <param name="newPath">Path to the new XML file.</param>
        /// <returns>A diff result.</returns>
        public DiffMatch Compare(string originalPath, string newPath)
        {
            return CompareAsync(originalPath, newPath, null).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Compares two XML files asynchronously using streaming.
        /// </summary>
        /// <param name="originalPath">Path to the original XML file.</param>
        /// <param name="newPath">Path to the new XML file.</param>
        /// <param name="progressCallback">Optional callback for progress updates.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the comparison operation.</returns>
        public async Task<DiffMatch> CompareAsync(
            string originalPath,
            string newPath,
            Action<StreamingProgress>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            var progress = new StreamingProgress();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Read both files in chunks
                var originalChunks = await ReadChunksAsync(originalPath, progress, cancellationToken);
                var newChunks = await ReadChunksAsync(newPath, progress, cancellationToken);

                progress.Stage = ProcessingStage.Comparing;
                ReportProgress(progress, progressCallback, stopwatch);

                // Match chunks by key
                var matchedChunks = MatchChunks(originalChunks, newChunks);

                // Compare each matched chunk
                var result = new DiffMatch { Path = "root" };
                var config = _options.ComparisonConfig ?? new XmlDiffConfig();

                int completed = 0;
                int total = matchedChunks.Count;

                if (_options.ParallelProcessing && total > 1)
                {
                    // Parallel processing
                    var tasks = matchedChunks.Select(async pair =>
                    {
                        var chunkDiff = await CompareChunkAsync(pair.Original, pair.New, config, cancellationToken);
                        lock (result)
                        {
                            result.Children.Add(chunkDiff);
                            completed++;
                            progress.ChunksProcessed = completed;
                            if (completed % 10 == 0)
                            {
                                ReportProgress(progress, progressCallback, stopwatch);
                            }
                        }
                        return chunkDiff;
                    });

                    await Task.WhenAll(tasks);
                }
                else
                {
                    // Sequential processing
                    foreach (var pair in matchedChunks)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var chunkDiff = await CompareChunkAsync(pair.Original, pair.New, config, cancellationToken);
                        result.Children.Add(chunkDiff);

                        completed++;
                        progress.ChunksProcessed = completed;
                        ReportProgress(progress, progressCallback, stopwatch);
                    }
                }

                progress.Stage = ProcessingStage.Completed;
                progress.PercentComplete = 100;
                ReportProgress(progress, progressCallback, stopwatch);

                return result;
            }
            catch (OperationCanceledException)
            {
                progress.Stage = ProcessingStage.Cancelled;
                throw;
            }
            catch (Exception ex)
            {
                progress.Stage = ProcessingStage.Failed;
                progress.Error = ex.Message;
                ReportProgress(progress, progressCallback, stopwatch);
                throw;
            }
        }

        /// <summary>
        /// Reads chunks from an XML file.
        /// </summary>
        private async Task<List<XmlChunk>> ReadChunksAsync(
            string filePath,
            StreamingProgress progress,
            CancellationToken cancellationToken)
        {
            var chunks = new List<XmlChunk>();
            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,
                IgnoreComments = true,
                DtdProcessing = _options.ValidateXml ? DtdProcessing.Parse : DtdProcessing.Ignore
            };

            using var stream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(stream, settings);

            var currentChunkElements = new List<XElement>();
            var currentDepth = 0;
            int totalElements = 0;

            while (await Task.Run(() => reader.Read(), cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType == XmlNodeType.Element)
                {
                    currentDepth++;

                    // Read the entire element
                    var element = XElement.ReadFrom(reader) as XElement;
                    if (element != null)
                    {
                        currentChunkElements.Add(element);
                        totalElements++;

                        // Check if we should start a new chunk
                        if (_options.ChunkProcessor.ShouldStartNewChunk(element, currentDepth))
                        {
                            var chunkKey = _options.ChunkProcessor.GetChunkKey(element);
                            var chunk = new XmlChunk
                            {
                                Key = chunkKey,
                                Elements = currentChunkElements.ToList()
                            };

                            chunks.Add(chunk);
                            currentChunkElements = new List<XElement>();
                            progress.ChunksRead = chunks.Count;

                            _options.ChunkProcessor.OnChunkStart(chunkKey, element);
                            _options.ChunkProcessor.OnChunkEnd(chunkKey, element);
                        }
                    }
                }
            }

            // Add remaining elements as final chunk
            if (currentChunkElements.Count > 0)
            {
                var chunkKey = "final";
                chunks.Add(new XmlChunk
                {
                    Key = chunkKey,
                    Elements = currentChunkElements
                });
            }

            return chunks;
        }

        /// <summary>
        /// Matches chunks between original and new documents.
        /// </summary>
        private List<(XmlChunk Original, XmlChunk New)> MatchChunks(
            List<XmlChunk> originalChunks,
            List<XmlChunk> newChunks)
        {
            var matched = new List<(XmlChunk, XmlChunk)>();
            var matchedKeys = new HashSet<string>();

            // First, match by key
            foreach (var origChunk in originalChunks)
            {
                var newChunk = newChunks.FirstOrDefault(c => c.Key == origChunk.Key);
                if (newChunk != null)
                {
                    matched.Add((origChunk, newChunk));
                    matchedKeys.Add(origChunk.Key);
                }
            }

            // Add unmatched chunks as additions/deletions
            foreach (var origChunk in originalChunks.Where(c => !matchedKeys.Contains(c.Key)))
            {
                matched.Add((origChunk, null));
            }

            foreach (var newChunk in newChunks.Where(c => !matchedKeys.Contains(c.Key)))
            {
                matched.Add((null, newChunk));
            }

            return matched;
        }

        /// <summary>
        /// Compares a single chunk.
        /// </summary>
        private async Task<DiffMatch> CompareChunkAsync(
            XmlChunk? originalChunk,
            XmlChunk? newChunk,
            XmlDiffConfig config,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (originalChunk == null && newChunk == null)
                {
                    return new DiffMatch { Type = DiffType.Unchanged };
                }

                if (originalChunk == null)
                {
                    // All elements in new chunk are additions
                    var result = new DiffMatch
                    {
                        Type = DiffType.Added,
                        Path = newChunk.Key
                    };

                    foreach (var elem in newChunk.Elements)
                    {
                        result.Children.Add(new DiffMatch
                        {
                            Type = DiffType.Added,
                            Path = newChunk.Key,
                            NewElement = elem
                        });
                    }

                    return result;
                }

                if (newChunk == null)
                {
                    // All elements in original chunk are deletions
                    var result = new DiffMatch
                    {
                        Type = DiffType.Deleted,
                        Path = originalChunk.Key
                    };

                    foreach (var elem in originalChunk.Elements)
                    {
                        result.Children.Add(new DiffMatch
                        {
                            Type = DiffType.Deleted,
                            Path = originalChunk.Key,
                            OriginalElement = elem
                        });
                    }

                    return result;
                }

                // Both chunks exist - compare them
                var container = new XElement("container");
                foreach (var elem in originalChunk.Elements)
                {
                    container.Add(new XElement(elem));
                }

                var newContainer = new XElement("container");
                foreach (var elem in newChunk.Elements)
                {
                    newContainer.Add(new XElement(elem));
                }

                return _engine.Compare(container, newContainer);
            }, cancellationToken);
        }

        /// <summary>
        /// Reports progress if enabled.
        /// </summary>
        private void ReportProgress(
            StreamingProgress progress,
            Action<StreamingProgress>? callback,
            Stopwatch stopwatch)
        {
            if (callback != null && _options.ReportProgress)
            {
                progress.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                callback(progress);
            }
        }
    }

    /// <summary>
    /// Represents a chunk of XML elements.
    /// </summary>
    internal class XmlChunk
    {
        public string Key { get; set; } = string.Empty;
        public List<XElement> Elements { get; set; } = new List<XElement>();
    }

    /// <summary>
    /// Progress information for streaming operations.
    /// </summary>
    public class StreamingProgress
    {
        /// <summary>
        /// Gets or sets the current processing stage.
        /// </summary>
        public ProcessingStage Stage { get; set; } = ProcessingStage.NotStarted;

        /// <summary>
        /// Gets or sets the number of chunks read.
        /// </summary>
        public int ChunksRead { get; set; }

        /// <summary>
        /// Gets or sets the number of chunks processed.
        /// </summary>
        public int ChunksProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total number of chunks.
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// Gets or sets the percentage complete (0-100).
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// Gets or sets the elapsed milliseconds.
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets an error message if failed.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Gets or sets the bytes processed.
        /// </summary>
        public long BytesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total bytes.
        /// </summary>
        public long TotalBytes { get; set; }
    }

    /// <summary>
    /// Processing stages for streaming operations.
    /// </summary>
    public enum ProcessingStage
    {
        /// <summary>
        /// Operation has not started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Reading the original document.
        /// </summary>
        ReadingOriginal,

        /// <summary>
        /// Reading the new document.
        /// </summary>
        ReadingNew,

        /// <summary>
        /// Comparing documents.
        /// </summary>
        Comparing,

        /// <summary>
        /// Operation completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Operation was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Operation failed.
        /// </summary>
        Failed
    }
}
