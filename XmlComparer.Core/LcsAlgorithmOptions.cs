namespace XmlComparer.Core
{
    /// <summary>
    /// Defines the LCS (Longest Common Subsequence) algorithm to use.
    /// </summary>
    /// <remarks>
    /// <para>LCS algorithms are used to find matching elements between XML documents.
    /// Different algorithms have different trade-offs between speed and memory usage.</para>
    /// </remarks>
    public enum LcsAlgorithmType
    {
        /// <summary>
        /// Classic dynamic programming approach with O(m*n) space and time.
        /// </summary>
        /// <remarks>
        /// Good for small to medium documents. Very stable and well-tested.
        /// </remarks>
        Standard,

        /// <summary>
        /// Hirschberg's algorithm with O(m*n) time but O(min(m,n)) space.
        /// </summary>
        /// <remarks>
        /// Best for large documents where memory is a concern. Uses divide-and-conquer
        /// to reduce space requirements.
        /// </remarks>
        Hirschberg,

        /// <summary>
        /// Myers' O(ND) diff algorithm (used by Git).
        /// </summary>
        /// <remarks>
        /// Very fast for documents with few differences. Slower when many differences exist.
        /// </remarks>
        Myers,

        /// <summary>
        /// Automatically selects the best algorithm based on input size.
        /// </summary>
        /// <remarks>
        /// Uses Standard for small inputs, Hirschberg for large inputs.
        /// </remarks>
        Auto
    }

    /// <summary>
    /// Configuration options for LCS algorithm selection and tuning.
    /// </summary>
    /// <remarks>
    /// <para>This class controls how the longest common subsequence (LCS) is calculated
    /// when comparing XML element sequences.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new LcsAlgorithmOptions
    /// {
    ///     Algorithm = LcsAlgorithmType.Auto,
    ///     AutoThreshold = 1000,
    ///     UseCaching = true
    /// };
    /// </code>
    /// </example>
    public class LcsAlgorithmOptions
    {
        /// <summary>
        /// Gets or sets the LCS algorithm to use.
        /// </summary>
        /// <remarks>
        /// Default is <see cref="LcsAlgorithmType.Auto"/>.
        /// </remarks>
        public LcsAlgorithmType Algorithm { get; set; } = LcsAlgorithmType.Auto;

        /// <summary>
        /// Gets or sets the threshold for auto-selecting algorithms.
        /// </summary>
        /// <remarks>
        /// When <see cref="Algorithm"/> is <see cref="LcsAlgorithmType.Auto"/>,
        /// inputs larger than this threshold will use Hirschberg's algorithm.
        /// Default is 1000 elements.
        /// </remarks>
        public int AutoThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to cache LCS results.
        /// </summary>
        /// <remarks>
        /// When true, previously computed LCS results are cached for reuse.
        /// This can speed up comparisons of similar documents but uses more memory.
        /// </remarks>
        public bool UseCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum cache size.
        /// </summary>
        /// <remarks>
        /// The maximum number of LCS results to cache. Default is 100.
        /// </remarks>
        public int MaxCacheSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to use parallel processing for large inputs.
        /// </summary>
        /// <remarks>
        /// When true and the input is large enough, the LCS computation may be
        /// parallelized across multiple threads. Default is false.
        /// </remarks>
        public bool UseParallelProcessing { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum input size for parallel processing.
        /// </summary>
        /// <remarks>
        /// Inputs smaller than this will always be processed sequentially.
        /// Default is 10000 elements.
        /// </remarks>
        public int ParallelThreshold { get; set; } = 10000;

        /// <summary>
        /// Creates options optimized for speed.
        /// </summary>
        /// <returns>Options configured for speed.</returns>
        public static LcsAlgorithmOptions ForSpeed() => new LcsAlgorithmOptions
        {
            Algorithm = LcsAlgorithmType.Myers,
            UseCaching = true,
            UseParallelProcessing = true,
            ParallelThreshold = 5000
        };

        /// <summary>
        /// Creates options optimized for memory.
        /// </summary>
        /// <returns>Options configured for low memory usage.</returns>
        public static LcsAlgorithmOptions ForMemory() => new LcsAlgorithmOptions
        {
            Algorithm = LcsAlgorithmType.Hirschberg,
            UseCaching = false,
            UseParallelProcessing = false
        };

        /// <summary>
        /// Creates options with balanced speed and memory.
        /// </summary>
        /// <returns>Options with balanced settings.</returns>
        public static LcsAlgorithmOptions Balanced() => new LcsAlgorithmOptions
        {
            Algorithm = LcsAlgorithmType.Auto,
            AutoThreshold = 1000,
            UseCaching = true,
            UseParallelProcessing = false
        };
    }
}
