using System;

namespace XmlComparer.Core
{
    /// <summary>
    /// Configuration options for streaming XML diff operations.
    /// </summary>
    /// <remarks>
    /// <para>Streaming diff processes XML documents in chunks rather than loading
    /// entire documents into memory. This enables comparison of very large files
    /// that would otherwise cause memory issues.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new StreamingDiffOptions
    /// {
    ///     ChunkProcessor = new ChunkProcessors.TopLevelElements(),
    ///     MaxChunkSize = 1024 * 1024, // 1MB per chunk
    ///     ParallelProcessing = true
    /// };
    ///
    /// var engine = new StreamingXmlDiffEngine(options);
    /// var diff = await engine.CompareAsync("large1.xml", "large2.xml");
    /// </code>
    /// </example>
    public class StreamingDiffOptions
    {
        /// <summary>
        /// Gets or sets the chunk processor strategy.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="ChunkProcessors.TopLevelElements"/>.
        /// </remarks>
        public IChunkProcessor ChunkProcessor { get; set; } = new ChunkProcessors.TopLevelElements();

        /// <summary>
        /// Gets or sets the maximum size of a chunk in bytes.
        /// </summary>
        /// <remarks>
        /// When a chunk exceeds this size, it will be split further.
        /// Default is 10MB.
        /// </remarks>
        public int MaxChunkSize { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum number of chunks to keep in memory.
        /// </summary>
        /// <remarks>
        /// When this limit is reached, older chunks are discarded.
        /// Default is 100.
        /// </remarks>
        public int MaxChunksInMemory { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to enable parallel chunk processing.
        /// </summary>
        /// <remarks>
        /// When true, multiple chunks are processed concurrently.
        /// Default is false.
        /// </remarks>
        public bool ParallelProcessing { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism.
        /// </summary>
        /// <remarks>
        /// The maximum number of chunks to process concurrently.
        /// Default is 4.
        /// </remarks>
        public int MaxDegreeOfParallelism { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to include progress reporting.
        /// </summary>
        /// <remarks>
        /// When true, progress events are raised during comparison.
        /// </remarks>
        public bool ReportProgress { get; set; } = false;

        /// <summary>
        /// Gets or sets the progress report interval in milliseconds.
        /// </summary>
        /// <remarks>
        /// Progress is reported at most once per interval.
        /// Default is 500ms.
        /// </remarks>
        public int ProgressReportInterval { get; set; } = 500;

        /// <summary>
        /// Gets or sets whether to use temporary files for intermediate results.
        /// </summary>
        /// <remarks>
        /// When true, chunks are written to temp files instead of memory.
        /// Useful for extremely large documents. Default is false.
        /// </remarks>
        public bool UseTempFiles { get; set; } = false;

        /// <summary>
        /// Gets or sets the directory for temporary files.
        /// </summary>
        /// <remarks>
        /// If null, the system temp directory is used.
        /// </remarks>
        public string? TempFileDirectory { get; set; }

        /// <summary>
        /// Gets or sets whether to validate XML during streaming.
        /// </summary>
        /// <remarks>
        /// When true, XML is validated against its schema/DTD.
        /// Default is false for performance.
        /// </remarks>
        public bool ValidateXml { get; set; } = false;

        /// <summary>
        /// Gets or sets the comparison configuration.
        /// </summary>
        public XmlDiffConfig? ComparisonConfig { get; set; }

        /// <summary>
        /// Creates options optimized for large files.
        /// </summary>
        /// <returns>Options configured for large file processing.</returns>
        public static StreamingDiffOptions ForLargeFiles() => new StreamingDiffOptions
        {
            MaxChunkSize = 5 * 1024 * 1024,
            MaxChunksInMemory = 50,
            ParallelProcessing = true,
            MaxDegreeOfParallelism = 8,
            UseTempFiles = true
        };

        /// <summary>
        /// Creates options optimized for memory efficiency.
        /// </summary>
        /// <returns>Options configured for low memory usage.</returns>
        public static StreamingDiffOptions ForLowMemory() => new StreamingDiffOptions
        {
            MaxChunkSize = 512 * 1024,
            MaxChunksInMemory = 10,
            ParallelProcessing = false,
            UseTempFiles = true
        };
    }
}
