using System;
using System.IO;
using System.Xml;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides secure XML reader settings to prevent XXE and other XML-based attacks.
    /// </summary>
    internal static class XmlSecuritySettings
    {
        // Security limits to prevent DoS attacks
        private const int MaxRecursionDepth = 1000;
        private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB
        private const int MaxCharactersFromEntities = 1024;
        private const int MaxCharactersInDocument = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Creates secure XmlReaderSettings with XXE protection and DoS limits.
        /// </summary>
        public static XmlReaderSettings CreateSecureReaderSettings()
        {
            return new XmlReaderSettings
            {
                // CRITICAL: Disable DTD processing to prevent XXE attacks
                DtdProcessing = DtdProcessing.Prohibit,

                // CRITICAL: Set XmlResolver to null to prevent external entity loading
                XmlResolver = null,

                // DoS protection limits
                MaxCharactersFromEntities = MaxCharactersFromEntities,
                MaxCharactersInDocument = MaxCharactersInDocument,

                // Ignore whitespace for consistency
                IgnoreWhitespace = true,
                IgnoreComments = true
            };
        }

        /// <summary>
        /// Validates a file path to prevent path traversal attacks.
        /// </summary>
        /// <param name="path">The file path to validate.</param>
        /// <param name="parameterName">The parameter name for exception messages.</param>
        /// <exception cref="ArgumentException">Thrown when path contains traversal sequences.</exception>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        public static void ValidatePath(string path, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", parameterName);
            }

            // Check for path traversal sequences
            if (path.Contains("..") || path.Contains("~"))
            {
                throw new ArgumentException(
                    "Path contains potentially dangerous traversal sequences. Absolute paths or relative paths without '..' are required.",
                    parameterName);
            }

            // Check for invalid characters
            char[] invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException("Path contains invalid characters.", parameterName);
            }
        }

        /// <summary>
        /// Validates file size before processing to prevent memory exhaustion.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <exception cref="InvalidOperationException">Thrown when file exceeds maximum size.</exception>
        public static void ValidateFileSize(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size of {MaxFileSizeBytes} bytes. " +
                    "Consider splitting the file or using streaming comparison.");
            }
        }

        /// <summary>
        /// Gets the maximum file size allowed for XML comparison.
        /// </summary>
        public static long GetMaxFileSizeBytes() => MaxFileSizeBytes;

        /// <summary>
        /// Gets the maximum recursion depth allowed for XML parsing.
        /// </summary>
        public static int GetMaxRecursionDepth() => MaxRecursionDepth;
    }
}
