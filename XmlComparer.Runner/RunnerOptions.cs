using System.Collections.Generic;

namespace XmlComparer.Runner
{
    /// <summary>
    /// Command-line options for the XmlComparer runner.
    /// </summary>
    public class RunnerOptions
    {
        /// <summary>
        /// Gets the positional arguments (file paths).
        /// </summary>
        public List<string> Positionals { get; } = new List<string>();

        /// <summary>
        /// Gets any parsing errors.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        // Output Options
        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the JSON output file path.
        /// </summary>
        public string? JsonOut { get; set; }

        /// <summary>
        /// Gets or sets whether to output JSON only.
        /// </summary>
        public bool JsonOnly { get; set; }

        /// <summary>
        /// Gets or sets the output format (html, text, json, markdown, csv).
        /// </summary>
        public string? OutputFormat { get; set; }

        // Comparison Options
        /// <summary>
        /// Gets or sets whether to run validation only.
        /// </summary>
        public bool ValidationOnly { get; set; }

        /// <summary>
        /// Gets or sets whether to ignore element values.
        /// </summary>
        public bool IgnoreValues { get; set; }

        /// <summary>
        /// Gets or sets whether to normalize whitespace.
        /// </summary>
        public bool NormalizeWhitespace { get; set; }

        /// <summary>
        /// Gets or sets whether to trim values.
        /// </summary>
        public bool TrimValues { get; set; }

        /// <summary>
        /// Gets or sets the key attributes for element matching.
        /// </summary>
        public List<string> KeyAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the elements to exclude.
        /// </summary>
        public List<string> ExcludeElements { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the attributes to exclude.
        /// </summary>
        public List<string> ExcludeAttributes { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the XSD schema paths for validation.
        /// </summary>
        public List<string> XsdPaths { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the namespace comparison mode.
        /// </summary>
        public string? NamespaceComparison { get; set; }

        // Configuration File
        /// <summary>
        /// Gets or sets the path to a configuration file.
        /// </summary>
        public string? ConfigFile { get; set; }

        /// <summary>
        /// Gets or sets whether to generate a sample configuration file.
        /// </summary>
        public bool GenerateSampleConfig { get; set; }

        // Batch Mode
        /// <summary>
        /// Gets or sets whether batch mode is enabled.
        /// </summary>
        public bool BatchMode { get; set; }

        /// <summary>
        /// Gets or sets the batch file path.
        /// </summary>
        public string? BatchFile { get; set; }

        /// <summary>
        /// Gets or sets the batch output directory.
        /// </summary>
        public string? BatchOutput { get; set; }

        /// <summary>
        /// Gets or sets the maximum parallelism for batch processing.
        /// </summary>
        public int BatchParallelism { get; set; } = 1;

        // Watch Mode
        /// <summary>
        /// Gets or sets whether watch mode is enabled.
        /// </summary>
        public bool WatchMode { get; set; }

        /// <summary>
        /// Gets or sets the watch debounce delay in milliseconds.
        /// </summary>
        public int WatchDebounce { get; set; } = 500;

        /// <summary>
        /// Gets or sets a command to run when changes are detected.
        /// </summary>
        public string? WatchAutoRun { get; set; }

        // Other
        /// <summary>
        /// Gets or sets whether to show help.
        /// </summary>
        public bool ShowHelp { get; set; }

        /// <summary>
        /// Gets or sets whether to show version information.
        /// </summary>
        public bool ShowVersion { get; set; }

        /// <summary>
        /// Gets or sets whether to enable verbose output.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets whether to use streaming mode for large files.
        /// </summary>
        public bool Streaming { get; set; }
    }
}
