using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using XmlComparer.Core;

namespace XmlComparer.Runner
{
    /// <summary>
    /// Monitors files for changes and performs automatic comparisons.
    /// </summary>
    /// <remarks>
    /// <para>The watch mode processor monitors one or more file pairs and automatically
    /// runs comparisons when changes are detected. This is useful for continuous
    /// integration scenarios or during development.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var processor = new WatchModeProcessor();
    ///
    /// var options = new WatchOptions
    /// {
    ///     DebounceMilliseconds = 500,
    ///     OnChange = (result) => Console.WriteLine($"Changes detected: {result.TotalChanges}")
    /// };
    ///
    /// // Start watching
    /// var cts = new CancellationTokenSource();
    /// await processor.WatchAsync("original.xml", "modified.xml", options, cts.Token);
    ///
    /// // Later, stop watching
    /// cts.Cancel();
    /// </code>
    /// </example>
    public class WatchModeProcessor
    {
        private readonly System.Timers.Timer _debounceTimer;

        /// <summary>
        /// Creates a new WatchModeProcessor.
        /// </summary>
        public WatchModeProcessor()
        {
            _debounceTimer = new System.Timers.Timer();
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
            _debounceTimer.AutoReset = false;
        }

        /// <summary>
        /// Watches a single file pair for changes.
        /// </summary>
        /// <param name="originalPath">Path to the original file.</param>
        /// <param name="newPath">Path to the new file.</param>
        /// <param name="options">Watch options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the watch operation.</returns>
        public async Task WatchAsync(
            string originalPath,
            string newPath,
            WatchOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= new WatchOptions();

            var watcher = new FileSystemWatcher(Path.GetDirectoryName(originalPath) ?? ".");
            watcher.Filter = Path.GetFileName(originalPath);

            var lastOriginalContent = await File.ReadAllTextAsync(originalPath, cancellationToken);
            var lastNewContent = await File.ReadAllTextAsync(newPath, cancellationToken);

            _currentOptions = options;
            _currentOriginalPath = originalPath;
            _currentNewPath = newPath;
            _cancellationToken = cancellationToken;

            watcher.Changed += async (s, e) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                // Restart debounce timer
                _debounceTimer.Interval = options.DebounceMilliseconds;
                _debounceTimer.Stop();
                _debounceTimer.Start();
            };

            watcher.EnableRaisingEvents = true;

            // Keep running until cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        /// <summary>
        /// Watches multiple file pairs for changes.
        /// </summary>
        public async Task WatchMultipleAsync(
            FilePair[] pairs,
            WatchOptions options,
            CancellationToken cancellationToken = default)
        {
            options ??= new WatchOptions();

            var tasks = pairs.Select(pair =>
                WatchAsync(pair.OriginalPath, pair.NewPath, options, cancellationToken));

            await Task.WhenAll(tasks);
        }

        // Timer callback fields
        private WatchOptions? _currentOptions;
        private string? _currentOriginalPath;
        private string? _currentNewPath;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Handles the debounce timer elapsed event.
        /// </summary>
        private async void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_currentOptions == null ||
                _currentOriginalPath == null ||
                _currentNewPath == null)
            {
                return;
            }

            try
            {
                // Perform comparison
                var originalXml = await File.ReadAllTextAsync(_currentOriginalPath, _cancellationToken);
                var newXml = await File.ReadAllTextAsync(_currentNewPath, _cancellationToken);

                var config = _currentOptions.Config?.ToComparisonOptions() ?? new XmlComparisonOptions();
                var comparer = new XmlComparerService(config.BuildConfig());
                var diff = comparer.CompareXml(originalXml, newXml);

                var result = new WatchResult
                {
                    OriginalPath = _currentOriginalPath,
                    NewPath = _currentNewPath,
                    Diff = diff,
                    Timestamp = DateTime.UtcNow
                };

                // Count changes
                result.TotalChanges = CountChanges(diff);
                result.HasChanges = result.TotalChanges > 0;

                // Invoke callback
                _currentOptions.OnChange?.Invoke(result);

                // Save output if configured
                if (result.HasChanges && !string.IsNullOrEmpty(_currentOptions.OutputPath))
                {
                    await SaveOutputAsync(result, _currentOptions, _cancellationToken);
                }

                // Auto-run command if configured
                if (result.HasChanges && !string.IsNullOrEmpty(_currentOptions.AutoRunCommand))
                {
                    await RunAutoCommandAsync(result, _currentOptions, _cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _currentOptions.OnError?.Invoke(ex);
            }
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
        /// Saves output for a watch result.
        /// </summary>
        private async Task SaveOutputAsync(WatchResult result, WatchOptions options, CancellationToken cancellationToken)
        {
            var formatter = new TextDiffFormatter();
            string output = formatter.Format(result.Diff);

            await File.WriteAllTextAsync(options.OutputPath!, output, cancellationToken);

            if (options.OnOutputSaved != null)
            {
                options.OnOutputSaved(options.OutputPath, result);
            }
        }

        /// <summary>
        /// Runs an auto command on change.
        /// </summary>
        private async Task RunAutoCommandAsync(WatchResult result, WatchOptions options, CancellationToken cancellationToken)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{options.AutoRunCommand}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                await process.WaitForExitAsync(cancellationToken);

                if (options.OnCommandCompleted != null)
                {
                    options.OnCommandCompleted(process.ExitCode, process.StandardOutput.ReadToEnd(), result);
                }
            }
            catch (Exception ex)
            {
                options.OnError?.Invoke(ex);
            }
        }
    }

    /// <summary>
    /// Options for watch mode processing.
    /// </summary>
    public class WatchOptions
    {
        /// <summary>
        /// Gets or sets the debounce delay in milliseconds.
        /// </summary>
        /// <remarks>
        /// Changes are only processed after this period of inactivity.
        /// Default is 500ms.
        /// </remarks>
        public int DebounceMilliseconds { get; set; } = 500;

        /// <summary>
        /// Gets or sets the output path for comparison results.
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the comparison configuration.
        /// </summary>
        public ComparisonConfiguration? Config { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when changes are detected.
        /// </summary>
        public Action<WatchResult>? OnChange { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when an error occurs.
        /// </summary>
        public Action<Exception>? OnError { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when output is saved.
        /// </summary>
        public Action<string, WatchResult>? OnOutputSaved { get; set; }

        /// <summary>
        /// Gets or sets a command to auto-run when changes are detected.
        /// </summary>
        public string? AutoRunCommand { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the auto-run command completes.
        /// </summary>
        public Action<int, string, WatchResult>? OnCommandCompleted { get; set; }

        /// <summary>
        /// Gets or sets whether to clear the output file on each run.
        /// </summary>
        public bool ClearOutputOnRun { get; set; } = true;
    }

    /// <summary>
    /// Result from a watch mode comparison.
    /// </summary>
    public class WatchResult
    {
        /// <summary>
        /// Gets or sets the original file path.
        /// </summary>
        public string? OriginalPath { get; set; }

        /// <summary>
        /// Gets or sets the new file path.
        /// </summary>
        public string? NewPath { get; set; }

        /// <summary>
        /// Gets or sets the diff result.
        /// </summary>
        public DiffMatch? Diff { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the comparison.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the total number of changes.
        /// </summary>
        public int TotalChanges { get; set; }

        /// <summary>
        /// Gets or sets whether changes were detected.
        /// </summary>
        public bool HasChanges { get; set; }
    }
}
