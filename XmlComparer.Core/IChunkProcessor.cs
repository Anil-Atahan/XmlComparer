using System;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Defines a strategy for processing chunks of XML during streaming comparison.
    /// </summary>
    /// <remarks>
    /// <para>Chunk processors are used by the streaming diff engine to handle
    /// portions of XML documents independently, enabling comparison of very
    /// large files that don't fit in memory.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomChunkProcessor : IChunkProcessor
    /// {
    ///     public bool ShouldStartNewChunk(XElement element, int currentDepth)
    ///     {
    ///         // Start a new chunk at each top-level element
    ///         return currentDepth == 1 && element.Parent == element.Document.Root;
    ///     }
    ///
    ///     public string GetChunkKey(XElement element)
    ///     {
    ///         // Use element name and ID attribute as key
    ///         var idAttr = element.Attribute("id");
    ///         return idAttr != null ? $"{element.Name.LocalName}:{idAttr.Value}" : element.Name.LocalName;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IChunkProcessor
    {
        /// <summary>
        /// Determines whether to start a new chunk at the current element.
        /// </summary>
        /// <param name="element">The current element being processed.</param>
        /// <param name="currentDepth">The current depth in the XML tree.</param>
        /// <returns>True if a new chunk should be started.</returns>
        /// <remarks>
        /// <para>This method is called for each element during streaming.
        /// Return true to indicate that this element should start a new chunk.</para>
        /// <para>Typical chunking strategies include:</para>
        /// <list type="bullet">
        ///   <item><description>By depth (e.g., chunk at depth 1)</description></item>
        ///   <item><description>By element name (e.g., chunk at specific element types)</description></item>
        ///   <item><description>By size (e.g., chunk after N elements)</description></item>
        ///   <item><description>By key attribute (e.g., chunk at each unique ID)</description></item>
        /// </list>
        /// </remarks>
        bool ShouldStartNewChunk(XElement element, int currentDepth);

        /// <summary>
        /// Gets a unique key for a chunk to enable matching across documents.
        /// </summary>
        /// <param name="element">The element representing the chunk.</param>
        /// <returns>A unique key for this chunk.</returns>
        /// <remarks>
        /// <para>The chunk key is used to match corresponding chunks between
        /// the original and new documents during streaming comparison.</para>
        /// <para>Common key strategies include:</para>
        /// <list type="bullet">
        ///   <item><description>Element name only (simple)</description></item>
        ///   <item><description>Element name + ID attribute (reliable)</description></item>
        ///   <item><description>Hash of element content (robust but slower)</description></item>
        /// </list>
        /// </remarks>
        string GetChunkKey(XElement element);

        /// <summary>
        /// Called when a chunk processing begins.
        /// </summary>
        /// <param name="chunkKey">The key of the chunk being processed.</param>
        /// <param name="element">The root element of the chunk.</param>
        /// <remarks>
        /// This can be used to initialize state for chunk processing.
        /// </remarks>
        void OnChunkStart(string chunkKey, XElement element);

        /// <summary>
        /// Called when a chunk processing ends.
        /// </summary>
        /// <param name="chunkKey">The key of the chunk that was processed.</param>
        /// <param name="element">The root element of the chunk.</param>
        /// <remarks>
        /// This can be used to clean up state after chunk processing.
        /// </remarks>
        void OnChunkEnd(string chunkKey, XElement element);
    }

    /// <summary>
    /// Predefined chunk processing strategies.
    /// </summary>
    public static class ChunkProcessors
    {
        /// <summary>
        /// Chunks XML by top-level elements (depth 1).
        /// </summary>
        /// <remarks>
        /// Each direct child of the root element becomes a separate chunk.
        /// This is the most common and simplest chunking strategy.
        /// </remarks>
        public class TopLevelElements : IChunkProcessor
        {
            /// <summary>
            /// Returns true for direct children of the root element.
            /// </summary>
            public bool ShouldStartNewChunk(XElement element, int currentDepth)
            {
                return currentDepth == 1 && element.Parent?.Parent == null;
            }

            /// <summary>
            /// Returns the element name as the chunk key.
            /// </summary>
            public string GetChunkKey(XElement element)
            {
                // Try to use ID attribute for better matching
                var idAttr = element.Attribute("id");
                if (idAttr != null)
                {
                    return $"{element.Name.LocalName}:{idAttr.Value}";
                }

                // Try other common ID attributes
                foreach (var attrName in new[] { "name", "key", "code", "ref" })
                {
                    var attr = element.Attribute(attrName);
                    if (attr != null)
                    {
                        return $"{element.Name.LocalName}:{attr.Value}";
                    }
                }

                return element.Name.LocalName;
            }

            /// <summary>
            /// Called when chunk processing starts.
            /// </summary>
            public void OnChunkStart(string chunkKey, XElement element)
            {
                // No-op
            }

            /// <summary>
            /// Called when chunk processing ends.
            /// </summary>
            public void OnChunkEnd(string chunkKey, XElement element)
            {
                // No-op
            }
        }

        /// <summary>
        /// Chunks XML by a specific depth level.
        /// </summary>
        /// <remarks>
        /// Creates a new chunk whenever the specified depth is reached.
        /// </remarks>
        public class ByDepth : IChunkProcessor
        {
            private readonly int _chunkDepth;

            /// <summary>
            /// Creates a new depth-based chunk processor.
            /// </summary>
            /// <param name="chunkDepth">The depth at which to create chunks (1 = root's children).</param>
            public ByDepth(int chunkDepth)
            {
                _chunkDepth = chunkDepth;
            }

            /// <summary>
            /// Returns true when the current depth equals the chunk depth.
            /// </summary>
            public bool ShouldStartNewChunk(XElement element, int currentDepth)
            {
                return currentDepth == _chunkDepth;
            }

            /// <summary>
            /// Returns the element name and path as the chunk key.
            /// </summary>
            public string GetChunkKey(XElement element)
            {
                var idAttr = element.Attribute("id");
                if (idAttr != null)
                {
                    return $"{element.Name.LocalName}:{idAttr.Value}";
                }
                return element.Name.LocalName;
            }

            /// <summary>
            /// Called when chunk processing starts.
            /// </summary>
            public void OnChunkStart(string chunkKey, XElement element)
            {
                // No-op
            }

            /// <summary>
            /// Called when chunk processing ends.
            /// </summary>
            public void OnChunkEnd(string chunkKey, XElement element)
            {
                // No-op
            }
        }

        /// <summary>
        /// Chunks XML by size (number of elements).
        /// </summary>
        /// <remarks>
        /// Creates a new chunk after processing a specified number of elements.
        /// Useful for controlling memory usage.
        /// </remarks>
        public class BySize : IChunkProcessor
        {
            private readonly int _maxElementsPerChunk;
            private int _elementCount;

            /// <summary>
            /// Creates a new size-based chunk processor.
            /// </summary>
            /// <param name="maxElementsPerChunk">Maximum elements per chunk.</param>
            public BySize(int maxElementsPerChunk)
            {
                _maxElementsPerChunk = maxElementsPerChunk;
                _elementCount = 0;
            }

            /// <summary>
            /// Returns true when the element count exceeds the threshold.
            /// </summary>
            public bool ShouldStartNewChunk(XElement element, int currentDepth)
            {
                _elementCount++;
                if (_elementCount >= _maxElementsPerChunk)
                {
                    _elementCount = 0;
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Returns a sequential key for chunks.
            /// </summary>
            public string GetChunkKey(XElement element)
            {
                return $"chunk-{_elementCount / _maxElementsPerChunk}";
            }

            /// <summary>
            /// Called when chunk processing starts.
            /// </summary>
            public void OnChunkStart(string chunkKey, XElement element)
            {
                // No-op
            }

            /// <summary>
            /// Called when chunk processing ends.
            /// </summary>
            public void OnChunkEnd(string chunkKey, XElement element)
            {
                // No-op
            }
        }

        /// <summary>
        /// Chunks XML by specific element names.
        /// </summary>
        /// <remarks>
        /// Creates a new chunk whenever an element with one of the specified names is encountered.
        /// </remarks>
        public class ByElementName : IChunkProcessor
        {
            private readonly HashSet<string> _chunkElementNames;

            /// <summary>
            /// Creates a new element-name-based chunk processor.
            /// </summary>
            /// <param name="chunkElementNames">Names of elements that should start chunks.</param>
            public ByElementName(params string[] chunkElementNames)
            {
                _chunkElementNames = new HashSet<string>(chunkElementNames);
            }

            /// <summary>
            /// Returns true if the element's name is in the chunk names list.
            /// </summary>
            public bool ShouldStartNewChunk(XElement element, int currentDepth)
            {
                return _chunkElementNames.Contains(element.Name.LocalName);
            }

            /// <summary>
            /// Returns the element name as the chunk key.
            /// </summary>
            public string GetChunkKey(XElement element)
            {
                var idAttr = element.Attribute("id");
                if (idAttr != null)
                {
                    return $"{element.Name.LocalName}:{idAttr.Value}";
                }
                return element.Name.LocalName;
            }

            /// <summary>
            /// Called when chunk processing starts.
            /// </summary>
            public void OnChunkStart(string chunkKey, XElement element)
            {
                // No-op
            }

            /// <summary>
            /// Called when chunk processing ends.
            /// </summary>
            public void OnChunkEnd(string chunkKey, XElement element)
            {
                // No-op
            }
        }
    }
}
