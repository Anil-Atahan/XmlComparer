using System.Text;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides optimized XML value normalization with cached patterns and pre-allocated buffers.
    /// </summary>
    internal static class XmlValueNormalizer
    {
        // Shared string builders for reuse (thread-static for thread safety)
        [ThreadStatic]
        private static StringBuilder? _cachedBuilder;

        // Pre-allocated capacity for common scenarios
        private const int DefaultBuilderCapacity = 256;

        /// <summary>
        /// Gets or creates a cached StringBuilder for the current thread.
        /// </summary>
        private static StringBuilder GetBuilder()
        {
            var builder = _cachedBuilder;
            if (builder == null)
            {
                builder = new StringBuilder(DefaultBuilderCapacity);
                _cachedBuilder = builder;
                return builder;
            }

            builder.Clear();
            return builder;
        }

        /// <summary>
        /// Normalizes an XML value according to the specified configuration.
        /// Uses optimized string operations to minimize allocations.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <param name="config">Configuration specifying normalization rules.</param>
        /// <returns>The normalized value.</returns>
        public static string Normalize(string value, XmlDiffConfig config)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Fast path: if no normalization needed, return as-is
            if (!config.NormalizeNewlines && !config.TrimValues && !config.NormalizeWhitespace)
            {
                return value;
            }

            var builder = GetBuilder();
            builder.Append(value);

            // Apply normalizations in order
            if (config.NormalizeNewlines)
            {
                NormalizeNewlines(builder);
            }

            if (config.TrimValues)
            {
                TrimValue(builder);
            }

            if (config.NormalizeWhitespace)
            {
                NormalizeWhitespace(builder);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Normalizes line endings to LF (\n).
        /// </summary>
        private static void NormalizeNewlines(StringBuilder builder)
        {
            // Replace CRLF with LF first
            builder.Replace("\r\n", "\n");

            // Then replace any remaining CR with LF
            builder.Replace("\r", "\n");
        }

        /// <summary>
        /// Trims leading and trailing whitespace.
        /// </summary>
        private static void TrimValue(StringBuilder builder)
        {
            if (builder.Length == 0) return;

            int start = 0;
            int end = builder.Length - 1;

            // Find first non-whitespace character
            while (start <= end && char.IsWhiteSpace(builder[start]))
            {
                start++;
            }

            // Find last non-whitespace character
            while (end >= start && char.IsWhiteSpace(builder[end]))
            {
                end--;
            }

            if (start > 0 || end < builder.Length - 1)
            {
                // Remove trailing characters first to avoid index shifting
                if (end < builder.Length - 1)
                {
                    builder.Remove(end + 1, builder.Length - end - 1);
                }

                // Remove leading characters
                if (start > 0)
                {
                    builder.Remove(0, start);
                }
            }
        }

        /// <summary>
        /// Normalizes whitespace by collapsing consecutive whitespace to a single space.
        /// Optimized implementation without regex for better performance.
        /// </summary>
        private static void NormalizeWhitespace(StringBuilder builder)
        {
            if (builder.Length == 0) return;

            int writePos = 0;
            bool inWhitespace = false;
            bool skipLeading = true;

            for (int readPos = 0; readPos < builder.Length; readPos++)
            {
                char c = builder[readPos];

                if (char.IsWhiteSpace(c))
                {
                    if (!inWhitespace && !skipLeading)
                    {
                        // First whitespace after non-whitespace - keep as single space
                        builder[writePos++] = ' ';
                        inWhitespace = true;
                    }
                    // Skip additional whitespace
                }
                else
                {
                    builder[writePos++] = c;
                    inWhitespace = false;
                    skipLeading = false;
                }
            }

            // Remove trailing space if added
            if (writePos > 0 && builder[writePos - 1] == ' ')
            {
                writePos--;
            }

            builder.Length = writePos;
        }
    }
}
