using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XmlComparer.Core;

namespace XmlComparer.Runner
{
    /// <summary>
    /// Loads XML comparison configuration from files.
    /// </summary>
    /// <remarks>
    /// <para>Configuration files allow you to save and reuse comparison settings.
    /// Supported formats include JSON and a simple key=value format.</para>
    /// </remarks>
    public class ConfigurationFileLoader
    {
        /// <summary>
        /// Loads configuration from a file.
        /// </summary>
        /// <param name="path">Path to the configuration file.</param>
        /// <returns>Loaded configuration options.</returns>
        public static ComparisonConfiguration Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Configuration file not found: {path}");
            }

            string extension = Path.GetExtension(path).ToLowerInvariant();

            return extension switch
            {
                ".json" => LoadJson(path),
                ".xmlconfig" => LoadKeyValue(path),
                ".config" => LoadKeyValue(path),
                _ => throw new NotSupportedException($"Unsupported configuration file format: {extension}")
            };
        }

        /// <summary>
        /// Loads configuration from a JSON file.
        /// </summary>
        private static ComparisonConfiguration LoadJson(string path)
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ComparisonConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return config ?? new ComparisonConfiguration();
        }

        /// <summary>
        /// Loads configuration from a key-value file.
        /// </summary>
        private static ComparisonConfiguration LoadKeyValue(string path)
        {
            var config = new ComparisonConfiguration();

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                int equalIndex = line.IndexOf('=');
                if (equalIndex <= 0)
                    continue;

                string key = line.Substring(0, equalIndex).Trim();
                string value = line.Substring(equalIndex + 1).Trim();

                switch (key.ToLowerInvariant())
                {
                    case "ignorevalues":
                        config.IgnoreValues = bool.Parse(value);
                        break;
                    case "keyattributes":
                        config.KeyAttributes = new List<string>(value.Split(',', StringSplitOptions.TrimEntries));
                        break;
                    case "excludeelements":
                        config.ExcludedElements = new List<string>(value.Split(',', StringSplitOptions.TrimEntries));
                        break;
                    case "excludeattributes":
                        config.ExcludedAttributes = new List<string>(value.Split(',', StringSplitOptions.TrimEntries));
                        break;
                    case "normalizewhitespace":
                        config.NormalizeWhitespace = bool.Parse(value);
                        break;
                    case "trimvalues":
                        config.TrimValues = bool.Parse(value);
                        break;
                    case "namespacecomparison":
                        config.NamespaceComparison = value;
                        break;
                    case "outputformat":
                        config.OutputFormat = value;
                        break;
                }
            }

            return config;
        }

        /// <summary>
        /// Saves configuration to a JSON file.
        /// </summary>
        public static void SaveJson(ComparisonConfiguration config, string path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Creates a sample configuration file.
        /// </summary>
        public static void CreateSample(string path)
        {
            var sample = new ComparisonConfiguration
            {
                IgnoreValues = false,
                NormalizeWhitespace = true,
                TrimValues = true,
                KeyAttributes = new List<string> { "id", "name" },
                ExcludedElements = new List<string> { "Comment", "Timestamp" },
                ExcludedAttributes = new List<string> { "lastModified" },
                NamespaceComparison = "UriSensitive",
                OutputFormat = "Html"
            };

            SaveJson(sample, path);
        }
    }

    /// <summary>
    /// Configuration file schema for XML comparisons.
    /// </summary>
    public class ComparisonConfiguration
    {
        /// <summary>
        /// Gets or sets whether to ignore element values.
        /// </summary>
        public bool IgnoreValues { get; set; }

        /// <summary>
        /// Gets or sets the key attributes for element matching.
        /// </summary>
        public List<string> KeyAttributes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the elements to exclude from comparison.
        /// </summary>
        public List<string> ExcludedElements { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the attributes to exclude from comparison.
        /// </summary>
        public List<string> ExcludedAttributes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to normalize whitespace.
        /// </summary>
        public bool NormalizeWhitespace { get; set; }

        /// <summary>
        /// Gets or sets whether to trim values.
        /// </summary>
        public bool TrimValues { get; set; }

        /// <summary>
        /// Gets or sets whether to normalize newlines.
        /// </summary>
        public bool NormalizeNewlines { get; set; }

        /// <summary>
        /// Gets or sets the namespace comparison mode.
        /// </summary>
        public string? NamespaceComparison { get; set; }

        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        public string? OutputFormat { get; set; }

        /// <summary>
        /// Gets or sets whether to exclude subtrees.
        /// </summary>
        public bool ExcludeSubtree { get; set; }

        /// <summary>
        /// Gets or sets whether to track prefix changes.
        /// </summary>
        public bool TrackPrefixChanges { get; set; }

        /// <summary>
        /// Converts this configuration to XmlComparisonOptions.
        /// </summary>
        public XmlComparisonOptions ToComparisonOptions()
        {
            var options = new XmlComparisonOptions();

            if (IgnoreValues)
                options.IgnoreValues();

            foreach (var attr in KeyAttributes)
                options.WithKeyAttributes(attr);

            foreach (var elem in ExcludedElements)
                options.ExcludingElement(elem);

            foreach (var attr in ExcludedAttributes)
                options.ExcludingAttribute(attr);

            if (NormalizeWhitespace)
                options.WithWhitespaceNormalization();

            if (TrimValues)
                options.WithValueTrimming();

            if (NormalizeNewlines)
                options.WithNewlineNormalization();

            if (!string.IsNullOrEmpty(NamespaceComparison) &&
                Enum.TryParse<NamespaceComparisonMode>(NamespaceComparison, out var nsMode))
            {
                options.WithNamespaceComparison(nsMode);
            }

            if (TrackPrefixChanges)
                options.TrackNamespacePrefixChanges();

            if (ExcludeSubtree)
                options.ExcludeSubtrees();

            return options;
        }
    }
}
