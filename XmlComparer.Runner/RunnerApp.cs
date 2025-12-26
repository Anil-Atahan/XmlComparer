using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XmlComparer.Core;

namespace XmlComparer.Runner
{
    /// <summary>
    /// Command-line application runner for XmlComparer.
    /// </summary>
    public static class RunnerApp
    {
        /// <summary>
        /// Parses command-line arguments.
        /// </summary>
        public static RunnerOptions ParseArgs(string[] args)
        {
            var options = new RunnerOptions();
            if (args.Length == 0) return options;

            if (args.Contains("--help") || args.Contains("-h"))
            {
                options.ShowHelp = true;
                return options;
            }

            if (args.Contains("--version") || args.Contains("-v"))
            {
                options.ShowVersion = true;
                return options;
            }

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    // Output options
                    case "--out":
                    case "--output":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.OutputPath = args[++i];
                        else
                            options.Errors.Add("Missing value for --out.");
                        break;

                    case "--format":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.OutputFormat = args[++i];
                        else
                            options.Errors.Add("Missing value for --format.");
                        break;

                    case "--json":
                        options.JsonOut = "diff.json";
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.JsonOut = args[++i];
                        break;

                    case "--json-only":
                        options.JsonOnly = true;
                        break;

                    // Comparison options
                    case "--validation-only":
                        options.ValidationOnly = true;
                        break;

                    case "--ignore-values":
                        options.IgnoreValues = true;
                        break;

                    case "--normalize-whitespace":
                        options.NormalizeWhitespace = true;
                        break;

                    case "--trim-values":
                        options.TrimValues = true;
                        break;

                    case "--key":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.KeyAttributes.AddRange(
                                args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        else
                            options.Errors.Add("Missing value for --key.");
                        break;

                    case "--exclude-elements":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.ExcludeElements.AddRange(
                                args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        else
                            options.Errors.Add("Missing value for --exclude-elements.");
                        break;

                    case "--exclude-attributes":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.ExcludeAttributes.AddRange(
                                args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        else
                            options.Errors.Add("Missing value for --exclude-attributes.");
                        break;

                    case "--xsd":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.XsdPaths.AddRange(
                                args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        else
                            options.Errors.Add("Missing value for --xsd.");
                        break;

                    case "--namespace":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.NamespaceComparison = args[++i];
                        else
                            options.Errors.Add("Missing value for --namespace.");
                        break;

                    // Configuration file
                    case "--config":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.ConfigFile = args[++i];
                        else
                            options.Errors.Add("Missing value for --config.");
                        break;

                    case "--generate-sample-config":
                        options.GenerateSampleConfig = true;
                        break;

                    // Batch mode
                    case "--batch":
                        options.BatchMode = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.BatchFile = args[++i];
                        break;

                    case "--batch-output":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.BatchOutput = args[++i];
                        else
                            options.Errors.Add("Missing value for --batch-output.");
                        break;

                    case "--parallel":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && int.TryParse(args[i + 1], out int parallel))
                        {
                            options.BatchParallelism = parallel;
                            i++;
                        }
                        else
                        {
                            options.BatchParallelism = 4;
                        }
                        break;

                    // Watch mode
                    case "--watch":
                        options.WatchMode = true;
                        break;

                    case "--watch-debounce":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-") && int.TryParse(args[i + 1], out int debounce))
                        {
                            options.WatchDebounce = debounce;
                            i++;
                        }
                        else
                        {
                            options.Errors.Add("Missing or invalid value for --watch-debounce.");
                        }
                        break;

                    case "--watch-run":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            options.WatchAutoRun = args[++i];
                        else
                            options.Errors.Add("Missing value for --watch-run.");
                        break;

                    // Other
                    case "--verbose":
                        options.Verbose = true;
                        break;

                    case "--streaming":
                        options.Streaming = true;
                        break;

                    default:
                        if (args[i].StartsWith("-", StringComparison.Ordinal))
                        {
                            options.Errors.Add($"Unknown option: {args[i]}");
                        }
                        else
                        {
                            options.Positionals.Add(args[i]);
                        }
                        break;
                }
            }

            return options;
        }

        /// <summary>
        /// Runs the comparison application with the specified options.
        /// </summary>
        public static async Task<int> Run(RunnerOptions options)
        {
            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            if (options.ShowVersion)
            {
                ShowVersion();
                return 0;
            }

            if (options.GenerateSampleConfig)
            {
                GenerateSampleConfig(options);
                return 0;
            }

            if (options.Errors.Count > 0)
            {
                foreach (var error in options.Errors)
                {
                    Console.Error.WriteLine($"Error: {error}");
                }
                return 1;
            }

            // Handle batch mode
            if (options.BatchMode)
            {
                return await RunBatchMode(options);
            }

            // Handle watch mode
            if (options.WatchMode)
            {
                return await RunWatchMode(options);
            }

            // Standard comparison mode
            return await RunComparison(options);
        }

        /// <summary>
        /// Runs batch mode.
        /// </summary>
        private static async Task<int> RunBatchMode(RunnerOptions options)
        {
            var processor = new BatchProcessor();

            if (string.IsNullOrEmpty(options.BatchFile))
            {
                Console.Error.WriteLine("Error: Batch mode requires --batch file.");
                return 1;
            }

            var batchOptions = new BatchOptions
            {
                MaxParallelism = options.BatchParallelism,
                OutputDirectory = options.BatchOutput,
                OutputFormat = options.OutputFormat,
                Config = LoadConfig(options)
            };

            var result = processor.ProcessBatch(options.BatchFile, batchOptions);

            Console.WriteLine($"Batch processing complete:");
            Console.WriteLine($"  Total pairs: {result.TotalPairs}");
            Console.WriteLine($"  Completed: {result.CompletedCount}");
            Console.WriteLine($"  Failed: {result.FailedCount}");
            Console.WriteLine($"  With differences: {result.PairsWithDifferences}");
            Console.WriteLine($"  Elapsed: {result.ElapsedMilliseconds}ms");

            return result.AllSucceeded ? 0 : 1;
        }

        /// <summary>
        /// Runs watch mode.
        /// </summary>
        private static async Task<int> RunWatchMode(RunnerOptions options)
        {
            if (options.Positionals.Count < 2)
            {
                Console.Error.WriteLine("Error: Watch mode requires two file paths.");
                return 1;
            }

            var processor = new WatchModeProcessor();
            var cts = new CancellationTokenSource();

            var watchOptions = new WatchOptions
            {
                DebounceMilliseconds = options.WatchDebounce,
                Config = LoadConfig(options),
                AutoRunCommand = options.WatchAutoRun,
                OnChange = (result) =>
                {
                    Console.WriteLine($"[{result.Timestamp:yyyy-MM-dd HH:mm:ss}] Changes detected: {result.TotalChanges} changes");
                    if (options.Verbose && result.Diff != null)
                    {
                        var formatter = new TextDiffFormatter();
                        Console.WriteLine(formatter.Format(result.Diff));
                    }
                },
                OnError = (ex) =>
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            };

            Console.WriteLine($"Watching for changes in:");
            Console.WriteLine($"  Original: {options.Positionals[0]}");
            Console.WriteLine($"  New: {options.Positionals[1]}");
            Console.WriteLine($"  Debounce: {options.WatchDebounce}ms");
            Console.WriteLine("Press Ctrl+C to stop.");

            // Handle Ctrl+C
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("\nStopping watch mode...");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                await processor.WatchAsync(
                    options.Positionals[0],
                    options.Positionals[1],
                    watchOptions,
                    cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Watch mode stopped.");
            }

            return 0;
        }

        /// <summary>
        /// Runs standard comparison mode.
        /// </summary>
        private static async Task<int> RunComparison(RunnerOptions options)
        {
            if (options.Positionals.Count < 2)
            {
                Console.Error.WriteLine("Error: Two file paths required.");
                Console.Error.WriteLine("Usage: xmlcomparer <original.xml> <new.xml> [options]");
                return 1;
            }

            var config = LoadConfig(options)?.ToComparisonOptions() ?? new XmlComparisonOptions();

            // Apply command-line options
            if (options.IgnoreValues) config.IgnoreValues();
            if (options.NormalizeWhitespace) config.WithWhitespaceNormalization();
            if (options.TrimValues) config.WithValueTrimming();
            foreach (var key in options.KeyAttributes) config.WithKeyAttributes(key);
            foreach (var elem in options.ExcludeElements) config.ExcludingElement(elem);
            foreach (var attr in options.ExcludeAttributes) config.ExcludingAttribute(attr);

            try
            {
                var originalXml = await System.IO.File.ReadAllTextAsync(options.Positionals[0]);
                var newXml = await System.IO.File.ReadAllTextAsync(options.Positionals[1]);

                var diff = new XmlComparerService(config.BuildConfig()).CompareXml(originalXml, newXml);

                // Output results
                if (!string.IsNullOrEmpty(options.OutputPath))
                {
                    var formatter = GetFormatter(options.OutputFormat ?? "html");
                    var output = formatter.Format(diff);
                    await System.IO.File.WriteAllTextAsync(options.OutputPath, output);
                    Console.WriteLine($"Output written to: {options.OutputPath}");
                }
                else
                {
                    var formatter = new TextDiffFormatter();
                    Console.WriteLine(formatter.Format(diff));
                }

                if (!string.IsNullOrEmpty(options.JsonOut))
                {
                    var jsonFormatter = new JsonDiffFormatter();
                    var json = jsonFormatter.Format(diff);
                    await System.IO.File.WriteAllTextAsync(options.JsonOut, json);
                    Console.WriteLine($"JSON output written to: {options.JsonOut}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (options.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        /// <summary>
        /// Loads configuration from file if specified.
        /// </summary>
        private static ComparisonConfiguration? LoadConfig(RunnerOptions options)
        {
            if (string.IsNullOrEmpty(options.ConfigFile))
                return null;

            try
            {
                return ConfigurationFileLoader.Load(options.ConfigFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Failed to load config file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a formatter for the specified format.
        /// </summary>
        private static IDiffFormatter GetFormatter(string format)
        {
            return format.ToLowerInvariant() switch
            {
                "json" => new JsonDiffFormatter(),
                "markdown" or "md" => new MarkdownDiffFormatter(),
                "csv" => new CsvDiffFormatter(),
                "text" or "txt" => new TextDiffFormatter(),
                "unified" or "diff" => new UnifiedDiffFormatter(),
                _ => new HtmlDiffFormatter()
            };
        }

        /// <summary>
        /// Generates a sample configuration file.
        /// </summary>
        private static void GenerateSampleConfig(RunnerOptions options)
        {
            string outputPath = options.ConfigFile ?? "xmlcomparer.config.json";
            ConfigurationFileLoader.CreateSample(outputPath);
            Console.WriteLine($"Sample configuration written to: {outputPath}");
        }

        /// <summary>
        /// Shows help information.
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("XmlComparer - Compare XML files and generate diff reports");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  xmlcomparer <original.xml> <new.xml> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help                    Show this help message");
            Console.WriteLine("  -v, --version                 Show version information");
            Console.WriteLine();
            Console.WriteLine("Output:");
            Console.WriteLine("  --out <file>                  Output file path");
            Console.WriteLine("  --format <type>               Output format (html, json, text, markdown, csv)");
            Console.WriteLine("  --json [file]                 Output JSON diff");
            Console.WriteLine("  --json-only                   Output JSON only");
            Console.WriteLine();
            Console.WriteLine("Comparison:");
            Console.WriteLine("  --validation-only             Validate XML only");
            Console.WriteLine("  --ignore-values               Ignore element values");
            Console.WriteLine("  --normalize-whitespace        Normalize whitespace");
            Console.WriteLine("  --trim-values                 Trim leading/trailing whitespace");
            Console.WriteLine("  --key <attrs>                 Key attributes for matching (comma-separated)");
            Console.WriteLine("  --exclude-elements <elems>    Elements to exclude (comma-separated)");
            Console.WriteLine("  --exclude-attributes <attrs>  Attributes to exclude (comma-separated)");
            Console.WriteLine("  --xsd <paths>                 XSD schemas for validation (comma-separated)");
            Console.WriteLine("  --namespace <mode>            Namespace comparison mode");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine("  --config <file>               Load options from configuration file");
            Console.WriteLine("  --generate-sample-config      Generate a sample configuration file");
            Console.WriteLine();
            Console.WriteLine("Batch Mode:");
            Console.WriteLine("  --batch [file]                Process multiple file pairs");
            Console.WriteLine("  --batch-output <dir>          Output directory for batch results");
            Console.WriteLine("  --parallel [n]                Max parallelism for batch (default: 4)");
            Console.WriteLine();
            Console.WriteLine("Watch Mode:");
            Console.WriteLine("  --watch                       Watch files for changes");
            Console.WriteLine("  --watch-debounce <ms>         Debounce delay in milliseconds (default: 500)");
            Console.WriteLine("  --watch-run <command>         Command to run when changes detected");
            Console.WriteLine();
            Console.WriteLine("Other:");
            Console.WriteLine("  --verbose                     Enable verbose output");
            Console.WriteLine("  --streaming                   Use streaming mode for large files");
        }

        /// <summary>
        /// Shows version information.
        /// </summary>
        private static void ShowVersion()
        {
            Console.WriteLine("XmlComparer version 1.0.0");
            Console.WriteLine("XML comparison and diff generation tool");
        }
    }
}
