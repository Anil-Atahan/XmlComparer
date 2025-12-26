using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides a fluent API for configuring XML comparison operations.
    /// </summary>
    /// <remarks>
    /// <para>This class implements the builder pattern to provide a fluent, chainable API for configuring comparison options.
    /// It encapsulates both the <see cref="XmlDiffConfig"/> for comparison behavior and output format settings.</para>
    /// <para><b>Fluent Configuration Example:</b></para>
    /// <code>
    /// var result = XmlComparer.CompareFilesWithReport("old.xml", "new.xml",
    ///     options => options
    ///         .WithKeyAttributes("id", "code")
    ///         .ExcludeAttributes("timestamp")
    ///         .NormalizeWhitespace()
    ///         .IncludeHtml()
    ///         .IncludeJson());
    /// </code>
    /// <para><b>Persisted Configuration Example:</b></para>
    /// <code>
    /// // Save configuration to file
    /// var options = new XmlComparisonOptions()
    ///     .WithKeyAttributes("id")
    ///     .IncludeHtml();
    /// options.SaveToFile("comparison-settings.json");
    ///
    /// // Load configuration from file
    /// var loaded = XmlComparisonOptions.LoadFromFile("comparison-settings.json");
    /// var result = XmlComparer.CompareFilesWithReport("old.xml", "new.xml",
    ///     o => o.WithKeyAttributes(loaded.Config.KeyAttributeNames.ToArray()));
    /// </code>
    /// </remarks>
    public class XmlComparisonOptions
    {
        /// <summary>
        /// Provides a registry for storing and retrieving custom matching strategies by identifier.
        /// </summary>
        /// <remarks>
        /// This static registry enables strategy serialization and deserialization by allowing
        /// strategies to be registered with a string identifier that can be persisted in configuration files.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Register a custom strategy
        /// MatchingStrategyRegistry.Register("fuzzy-match", () => new FuzzyMatchingStrategy());
        ///
        /// // Use in options
        /// var options = new XmlComparisonOptions()
        ///     .UseMatchingStrategy(new FuzzyMatchingStrategy(), "fuzzy-match");
        ///
        /// // Later, resolve from registry
        /// options.ResolveMatchingStrategyFromRegistry();
        /// </code>
        /// </example>
        public static class MatchingStrategyRegistry
        {
            private static readonly ConcurrentDictionary<string, Func<IMatchingStrategy>> _factories = new();

            /// <summary>
            /// Registers a matching strategy factory with the specified identifier.
            /// </summary>
            /// <param name="id">The unique identifier for this strategy.</param>
            /// <param name="factory">A factory function that creates new instances of the strategy.</param>
            /// <exception cref="ArgumentException">Thrown when id is null or whitespace.</exception>
            /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
            /// <remarks>
            /// If a strategy with the same id already exists, it will be overwritten.
            /// Use <see cref="TryRegister"/> to avoid replacing existing strategies.
            /// </remarks>
            public static void Register(string id, Func<IMatchingStrategy> factory)
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Strategy id is required.", nameof(id));
                // Overwrites are allowed; use TryRegister to avoid replacing.
                _factories[id] = factory ?? throw new ArgumentNullException(nameof(factory));
            }

            /// <summary>
            /// Attempts to register a matching strategy factory without overwriting existing entries.
            /// </summary>
            /// <param name="id">The unique identifier for this strategy.</param>
            /// <param name="factory">A factory function that creates new instances of the strategy.</param>
            /// <returns>True if the strategy was registered; false if the id was already registered or parameters were invalid.</returns>
            public static bool TryRegister(string id, Func<IMatchingStrategy> factory)
            {
                if (string.IsNullOrWhiteSpace(id)) return false;
                if (factory == null) return false;
                return _factories.TryAdd(id, factory);
            }

            /// <summary>
            /// Resolves a matching strategy instance from its identifier.
            /// </summary>
            /// <param name="id">The identifier of the strategy to resolve.</param>
            /// <returns>A new instance of the matching strategy, or null if not found.</returns>
            public static IMatchingStrategy? Resolve(string id)
            {
                if (string.IsNullOrWhiteSpace(id)) return null;
                return _factories.TryGetValue(id, out var factory) ? factory() : null;
            }

            /// <summary>
            /// Attempts to resolve a matching strategy instance from its identifier.
            /// </summary>
            /// <param name="id">The identifier of the strategy to resolve.</param>
            /// <param name="strategy">When this method returns, contains the resolved strategy or null.</param>
            /// <returns>True if the strategy was found; false otherwise.</returns>
            public static bool TryResolve(string id, out IMatchingStrategy? strategy)
            {
                strategy = null;
                if (string.IsNullOrWhiteSpace(id)) return false;
                if (_factories.TryGetValue(id, out var factory))
                {
                    strategy = factory();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Removes a matching strategy from the registry.
            /// </summary>
            /// <param name="id">The identifier of the strategy to remove.</param>
            /// <returns>True if the strategy was removed; false if not found.</returns>
            public static bool Unregister(string id)
            {
                if (string.IsNullOrWhiteSpace(id)) return false;
                return _factories.TryRemove(id, out _);
            }

            /// <summary>
            /// Clears all registered matching strategies from the registry.
            /// </summary>
            public static void Clear()
            {
                _factories.Clear();
            }
        }

        /// <summary>
        /// Gets the core comparison configuration.
        /// </summary>
        public XmlDiffConfig Config { get; } = new XmlDiffConfig();

        /// <summary>
        /// Gets or sets the matching strategy for element comparison.
        /// </summary>
        public IMatchingStrategy? MatchingStrategy { get; private set; }

        /// <summary>
        /// Gets the identifier for the matching strategy, used for serialization and registry resolution.
        /// </summary>
        public string? MatchingStrategyId { get; private set; }

        /// <summary>
        /// Gets the list of XSD file paths for XML validation.
        /// </summary>
        public List<string> XsdPaths { get; } = new List<string>();

        /// <summary>
        /// Gets or sets whether to generate an HTML report.
        /// </summary>
        public bool GenerateHtml { get; private set; } = false;

        /// <summary>
        /// Gets or sets whether to generate a JSON report.
        /// </summary>
        public bool GenerateJson { get; private set; } = false;

        /// <summary>
        /// Gets or sets whether to embed the JSON report within the HTML report.
        /// </summary>
        public bool EmbedJsonInHtml { get; private set; } = true;

        /// <summary>
        /// Gets or sets whether to include validation results in the JSON report.
        /// </summary>
        public bool IncludeValidationInJson { get; private set; } = true;

        /// <summary>
        /// Specifies attributes to use as keys for element matching.
        /// </summary>
        /// <param name="names">The attribute names to use as keys.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// When key attributes are specified, elements are matched by their key attribute values
        /// rather than by position. This enables move detection.
        /// </remarks>
        /// <example>
        /// <code>
        /// options.WithKeyAttributes("id", "code", "sku");
        /// </code>
        /// </example>
        public XmlComparisonOptions WithKeyAttributes(params string[] names)
        {
            foreach (var name in names)
            {
                Config.KeyAttributeNames.Add(name);
            }
            return this;
        }

        /// <summary>
        /// Specifies attributes to exclude from comparison.
        /// </summary>
        /// <param name="names">The attribute names to exclude.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// options.ExcludeAttributes("timestamp", "lastModified");
        /// </code>
        /// </example>
        public XmlComparisonOptions ExcludeAttributes(params string[] names)
        {
            foreach (var name in names)
            {
                Config.ExcludedAttributeNames.Add(name);
            }
            return this;
        }

        /// <summary>
        /// Specifies attributes to exclude from comparison.
        /// </summary>
        /// <param name="names">The attribute names to exclude.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// This is an alias for <see cref="ExcludeAttributes"/>.
        /// </remarks>
        public XmlComparisonOptions ExcludingAttribute(params string[] names)
        {
            return ExcludeAttributes(names);
        }

        /// <summary>
        /// Specifies nodes to exclude from comparison.
        /// </summary>
        /// <param name="excludeSubtree">If true, excludes the entire subtree; if false, only excludes the node itself.</param>
        /// <param name="names">The node names to exclude.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// // Exclude entire comment subtrees
        /// options.ExcludeNodes(true, "Comment");
        ///
        /// // Exclude timestamp nodes but keep their children
        /// options.ExcludeNodes(false, "Timestamp");
        /// </code>
        /// </example>
        public XmlComparisonOptions ExcludeNodes(bool excludeSubtree, params string[] names)
        {
            Config.ExcludeSubtree = excludeSubtree;
            foreach (var name in names)
            {
                Config.ExcludedNodeNames.Add(name);
            }
            return this;
        }

        /// <summary>
        /// Specifies elements to exclude from comparison (excludes the element itself but keeps its children).
        /// </summary>
        /// <param name="names">The element names to exclude.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// This is an alias for <see cref="ExcludeNodes"/> with excludeSubtree=false.
        /// </remarks>
        public XmlComparisonOptions ExcludingElement(params string[] names)
        {
            return ExcludeNodes(false, names);
        }

        /// <summary>
        /// Specifies entire subtrees to exclude from comparison.
        /// </summary>
        /// <param name="names">The node names whose entire subtrees should be excluded.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// This is an alias for <see cref="ExcludeNodes"/> with excludeSubtree=true.
        /// </remarks>
        public XmlComparisonOptions ExcludeSubtrees(params string[] names)
        {
            return ExcludeNodes(true, names);
        }

        /// <summary>
        /// Configures how XML namespaces are handled during comparison.
        /// </summary>
        /// <param name="mode">The namespace comparison mode.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// <para>Controls how namespace differences affect element matching and diff detection.</para>
        /// <para>The default mode is <see cref="NamespaceComparisonMode.Ignore"/>, which treats
        /// elements with different namespaces as equal if their local names match (backward compatible).</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Require exact namespace match (both URI and prefix)
        /// options.WithNamespaceComparison(NamespaceComparisonMode.Strict);
        ///
        /// // Track namespace URI changes but ignore prefix differences
        /// options.WithNamespaceComparison(NamespaceComparisonMode.UriSensitive);
        ///
        /// // Track prefix changes but ignore namespace URI differences
        /// options.WithNamespaceComparison(NamespaceComparisonMode.PrefixPreserve);
        /// </code>
        /// </example>
        public XmlComparisonOptions WithNamespaceComparison(NamespaceComparisonMode mode)
        {
            Config.NamespaceComparisonMode = mode;
            return this;
        }

        /// <summary>
        /// Configures whether to track namespace prefix changes as modifications.
        /// </summary>
        /// <param name="trackPrefixChanges">True to track prefix changes; false to ignore them.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// <para>When true and <see cref="XmlDiffConfig.NamespaceComparisonMode"/> is set to
        /// <see cref="NamespaceComparisonMode.UriSensitive"/>, namespace prefix changes
        /// (without URI changes) will be flagged as <see cref="DiffType.Modified"/> instead of ignored.</para>
        /// <para>This is useful when documenting namespace changes in XML schemas where
        /// the prefix itself matters for downstream processing.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Track prefix changes when using URI-sensitive mode
        /// options.WithNamespaceComparison(NamespaceComparisonMode.UriSensitive)
        ///        .TrackNamespacePrefixChanges();
        /// </code>
        /// </example>
        public XmlComparisonOptions TrackNamespacePrefixChanges(bool trackPrefixChanges = true)
        {
            Config.TrackPrefixChanges = trackPrefixChanges;
            return this;
        }

        /// <summary>
        /// Configures preservation and comparison of non-element XML nodes.
        /// </summary>
        /// <param name="settings">The node preservation settings.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// <para>By default, CDATA sections, comments, and processing instructions are stripped during
        /// document loading. Use this method to enable preservation of these nodes for comparison.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Preserve all non-element nodes
        /// options.PreserveNodes(XmlNodePreservationSettings.PreserveAll());
        ///
        /// // Preserve only comments
        /// options.PreserveNodes(XmlNodePreservationSettings.PreserveCommentsOnly());
        ///
        /// // Custom settings
        /// options.PreserveNodes(new XmlNodePreservationSettings
        /// {
        ///     Mode = XmlNodePreservationMode.CommentsOnly,
        ///     TrackCommentPosition = true
        /// });
        /// </code>
        /// </example>
        public XmlComparisonOptions PreserveNodes(XmlNodePreservationSettings settings)
        {
            Config.NodePreservation = settings ?? throw new ArgumentNullException(nameof(settings));
            return this;
        }

        /// <summary>
        /// Configures preservation of non-element XML nodes using the specified mode.
        /// </summary>
        /// <param name="mode">The node preservation mode.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// // Preserve all non-element nodes
        /// options.PreserveNodes(XmlNodePreservationMode.PreserveAll);
        ///
        /// // Preserve only comments
        /// options.PreserveNodes(XmlNodePreservationMode.CommentsOnly);
        /// </code>
        /// </example>
        public XmlComparisonOptions PreserveNodes(XmlNodePreservationMode mode)
        {
            Config.NodePreservation = new XmlNodePreservationSettings { Mode = mode };
            return this;
        }

        /// <summary>
        /// Adds a custom value normalizer to apply during comparison.
        /// </summary>
        /// <param name="normalizer">The value normalizer to add.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// <para>Custom normalizers are applied after the built-in normalizers (whitespace, newlines, trimming).
        /// Each normalizer is applied in sequence to transform values before comparison.</para>
        /// <para>Common normalizers include <see cref="DateTimeNormalizer"/>, <see cref="NumericNormalizer"/>,
        /// <see cref="BooleanNormalizer"/>, and <see cref="RegexNormalizer"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.UseValueNormalizer(new DateTimeNormalizer("yyyy-MM-dd"))
        ///        .UseValueNormalizer(new NumericNormalizer(3))
        ///        .UseValueNormalizer(new BooleanNormalizer());
        /// </code>
        /// </example>
        public XmlComparisonOptions UseValueNormalizer(IValueNormalizer normalizer)
        {
            Config.ValueNormalizers.Add(normalizer ?? throw new ArgumentNullException(nameof(normalizer)));
            return this;
        }

        /// <summary>
        /// Adds a value normalizer and registers it with a specific identifier.
        /// </summary>
        /// <param name="normalizer">The value normalizer to add and register.</param>
        /// <param name="id">The identifier to register the normalizer with.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// <para>This method both adds the normalizer to the config and registers it in the
        /// <see cref="ValueNormalizerRegistry"/> for later retrieval.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// options.UseValueNormalizer(new DateTimeNormalizer(), "date-normalizer");
        ///
        /// // Later, resolve from registry
        /// var normalizer = ValueNormalizerRegistry.Resolve("date-normalizer");
        /// </code>
        /// </example>
        public XmlComparisonOptions UseValueNormalizer(IValueNormalizer normalizer, string id)
        {
            Config.ValueNormalizers.Add(normalizer ?? throw new ArgumentNullException(nameof(normalizer)));
            ValueNormalizerRegistry.Register(id, normalizer);
            return this;
        }

        /// <summary>
        /// Configures whether to ignore element values during comparison.
        /// </summary>
        /// <param name="ignore">True to ignore values; false to compare them.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// When true, only structure and attributes are compared, not text content.
        /// </remarks>
        public XmlComparisonOptions IgnoreValues(bool ignore = true)
        {
            Config.IgnoreValues = ignore;
            return this;
        }

        /// <summary>
        /// Configures whitespace normalization during value comparison.
        /// </summary>
        /// <param name="normalize">True to normalize whitespace; false to preserve it.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// Whitespace normalization collapses multiple spaces and tabs into single spaces.
        /// </remarks>
        public XmlComparisonOptions NormalizeWhitespace(bool normalize = true)
        {
            Config.NormalizeWhitespace = normalize;
            return this;
        }

        /// <summary>
        /// Enables whitespace normalization during value comparison.
        /// </summary>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// This is an alias for <see cref="NormalizeWhitespace(bool)"/>.
        /// </remarks>
        public XmlComparisonOptions WithWhitespaceNormalization()
        {
            return NormalizeWhitespace(true);
        }

        /// <summary>
        /// Configures leading/trailing whitespace trimming during value comparison.
        /// </summary>
        /// <param name="trim">True to trim values; false to preserve whitespace.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        public XmlComparisonOptions TrimValues(bool trim = true)
        {
            Config.TrimValues = trim;
            return this;
        }

        /// <summary>
        /// Enables leading/trailing whitespace trimming during value comparison.
        /// </summary>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// This is an alias for <see cref="TrimValues(bool)"/>.
        /// </remarks>
        public XmlComparisonOptions WithValueTrimming()
        {
            return TrimValues(true);
        }

        /// <summary>
        /// Configures newline normalization during value comparison.
        /// </summary>
        /// <param name="normalize">True to normalize newlines; false to preserve them.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// Newline normalization converts all line ending styles (CRLF, LF, CR) to a consistent format.
        /// </remarks>
        public XmlComparisonOptions NormalizeNewlines(bool normalize = true)
        {
            Config.NormalizeNewlines = normalize;
            return this;
        }

        /// <summary>
        /// Enables newline normalization during value comparison.
        /// </summary>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <remarks>
        /// This is an alias for <see cref="NormalizeNewlines(bool)"/>.
        /// </remarks>
        public XmlComparisonOptions WithNewlineNormalization()
        {
            return NormalizeNewlines(true);
        }

        /// <summary>
        /// Sets a custom matching strategy for element comparison.
        /// </summary>
        /// <param name="strategy">The matching strategy to use.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// options.UseMatchingStrategy(new FuzzyMatchingStrategy());
        /// </code>
        /// </example>
        public XmlComparisonOptions UseMatchingStrategy(IMatchingStrategy strategy)
        {
            MatchingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Sets a custom matching strategy and registers it with an identifier for serialization.
        /// </summary>
        /// <param name="strategy">The matching strategy to use.</param>
        /// <param name="strategyId">The identifier for this strategy.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// options.UseMatchingStrategy(new FuzzyMatchingStrategy(), "fuzzy-match");
        /// </code>
        /// </example>
        public XmlComparisonOptions UseMatchingStrategy(IMatchingStrategy strategy, string strategyId)
        {
            MatchingStrategy = strategy;
            MatchingStrategyId = strategyId;
            return this;
        }

        /// <summary>
        /// Adds XSD schema files for XML validation.
        /// </summary>
        /// <param name="xsdPaths">The paths to the XSD files.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// options.ValidateWithXsds("schema1.xsd", "schema2.xsd");
        /// </code>
        /// </example>
        public XmlComparisonOptions ValidateWithXsds(params string[] xsdPaths)
        {
            XsdPaths.AddRange(xsdPaths);
            return this;
        }

        /// <summary>
        /// Configures HTML report generation.
        /// </summary>
        /// <param name="include">True to generate HTML; false otherwise.</param>
        /// <param name="embedJson">True to embed the JSON report in the HTML; false to keep separate.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// options.IncludeHtml();  // Generate HTML with embedded JSON
        /// options.IncludeHtml(true, false);  // Generate HTML without embedded JSON
        /// </code>
        /// </example>
        public XmlComparisonOptions IncludeHtml(bool include = true, bool embedJson = true)
        {
            GenerateHtml = include;
            EmbedJsonInHtml = embedJson;
            return this;
        }

        /// <summary>
        /// Configures JSON report generation.
        /// </summary>
        /// <param name="include">True to generate JSON; false otherwise.</param>
        /// <param name="includeValidation">True to include validation results in the JSON.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// options.IncludeJson();  // Generate JSON with validation
        /// options.IncludeJson(true, false);  // Generate JSON without validation
        /// </code>
        /// </example>
        public XmlComparisonOptions IncludeJson(bool include = true, bool includeValidation = true)
        {
            GenerateJson = include;
            IncludeValidationInJson = includeValidation;
            return this;
        }

        /// <summary>
        /// Serializes the options to JSON format.
        /// </summary>
        /// <returns>A JSON string representation of these options.</returns>
        /// <example>
        /// <code>
        /// var options = new XmlComparisonOptions().WithKeyAttributes("id").IncludeHtml();
        /// string json = options.ToJson();
        /// File.WriteAllText("settings.json", json);
        /// </code>
        /// </example>
        public string ToJson()
        {
            var dto = new XmlComparisonOptionsDto
            {
                Version = 1,
                ExcludedNodeNames = new List<string>(Config.ExcludedNodeNames),
                ExcludedAttributeNames = new List<string>(Config.ExcludedAttributeNames),
                KeyAttributeNames = new List<string>(Config.KeyAttributeNames),
                ExcludeSubtree = Config.ExcludeSubtree,
                NormalizeWhitespace = Config.NormalizeWhitespace,
                TrimValues = Config.TrimValues,
                NormalizeNewlines = Config.NormalizeNewlines,
                IgnoreValues = Config.IgnoreValues,
                XsdPaths = new List<string>(XsdPaths),
                GenerateHtml = GenerateHtml,
                GenerateJson = GenerateJson,
                EmbedJsonInHtml = EmbedJsonInHtml,
                IncludeValidationInJson = IncludeValidationInJson,
                MatchingStrategyId = MatchingStrategyId
            };

            return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Saves the options to a file in JSON format.
        /// </summary>
        /// <param name="path">The file path to save to.</param>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        /// <example>
        /// <code>
        /// var options = new XmlComparisonOptions().WithKeyAttributes("id");
        /// options.SaveToFile("comparison-settings.json");
        /// </code>
        /// </example>
        public void SaveToFile(string path)
        {
            File.WriteAllText(path, ToJson());
        }

        /// <summary>
        /// Deserializes options from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A new <see cref="XmlComparisonOptions"/> instance with the deserialized settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the JSON is invalid or the version is unsupported.</exception>
        /// <example>
        /// <code>
        /// string json = File.ReadAllText("settings.json");
        /// var options = XmlComparisonOptions.FromJson(json);
        /// </code>
        /// </example>
        public static XmlComparisonOptions FromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<XmlComparisonOptionsDto>(json)
                      ?? throw new InvalidOperationException("Invalid options JSON.");

            int version = dto.Version == 0 ? 1 : dto.Version;
            if (version > 1)
            {
                throw new InvalidOperationException($"Unsupported options version: {version}");
            }

            var options = new XmlComparisonOptions();
            options.Config.ExcludeSubtree = dto.ExcludeSubtree;
            options.Config.NormalizeWhitespace = dto.NormalizeWhitespace;
            options.Config.TrimValues = dto.TrimValues;
            options.Config.NormalizeNewlines = dto.NormalizeNewlines;
            options.Config.IgnoreValues = dto.IgnoreValues;
            options.MatchingStrategyId = dto.MatchingStrategyId;

            foreach (var name in dto.ExcludedNodeNames ?? new List<string>())
            {
                options.Config.ExcludedNodeNames.Add(name);
            }
            foreach (var name in dto.ExcludedAttributeNames ?? new List<string>())
            {
                options.Config.ExcludedAttributeNames.Add(name);
            }
            foreach (var name in dto.KeyAttributeNames ?? new List<string>())
            {
                options.Config.KeyAttributeNames.Add(name);
            }
            foreach (var path in dto.XsdPaths ?? new List<string>())
            {
                options.XsdPaths.Add(path);
            }

            options.GenerateHtml = dto.GenerateHtml;
            options.GenerateJson = dto.GenerateJson;
            options.EmbedJsonInHtml = dto.EmbedJsonInHtml;
            options.IncludeValidationInJson = dto.IncludeValidationInJson;

            return options;
        }

        /// <summary>
        /// Deserializes options from a JSON string with a warning handler for backward compatibility issues.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="warningHandler">An action to handle warning messages.</param>
        /// <returns>A new <see cref="XmlComparisonOptions"/> instance with the deserialized settings.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the JSON is invalid or the version is unsupported.</exception>
        /// <example>
        /// <code>
        /// var options = XmlComparisonOptions.FromJson(json, warning => Console.WriteLine($"Warning: {warning}"));
        /// </code>
        /// </example>
        public static XmlComparisonOptions FromJson(string json, Action<string> warningHandler)
        {
            var dto = JsonSerializer.Deserialize<XmlComparisonOptionsDto>(json)
                      ?? throw new InvalidOperationException("Invalid options JSON.");

            int version = dto.Version == 0 ? 1 : dto.Version;
            if (dto.Version == 0)
            {
                warningHandler?.Invoke("Options JSON did not include a Version; treating as version 1.");
            }
            if (version > 1)
            {
                throw new InvalidOperationException($"Unsupported options version: {version}");
            }

            var options = new XmlComparisonOptions();
            options.Config.ExcludeSubtree = dto.ExcludeSubtree;
            options.Config.NormalizeWhitespace = dto.NormalizeWhitespace;
            options.Config.TrimValues = dto.TrimValues;
            options.Config.NormalizeNewlines = dto.NormalizeNewlines;
            options.Config.IgnoreValues = dto.IgnoreValues;
            options.MatchingStrategyId = dto.MatchingStrategyId;

            foreach (var name in dto.ExcludedNodeNames ?? new List<string>())
            {
                options.Config.ExcludedNodeNames.Add(name);
            }
            foreach (var name in dto.ExcludedAttributeNames ?? new List<string>())
            {
                options.Config.ExcludedAttributeNames.Add(name);
            }
            foreach (var name in dto.KeyAttributeNames ?? new List<string>())
            {
                options.Config.KeyAttributeNames.Add(name);
            }
            foreach (var path in dto.XsdPaths ?? new List<string>())
            {
                options.XsdPaths.Add(path);
            }

            options.GenerateHtml = dto.GenerateHtml;
            options.GenerateJson = dto.GenerateJson;
            options.EmbedJsonInHtml = dto.EmbedJsonInHtml;
            options.IncludeValidationInJson = dto.IncludeValidationInJson;

            return options;
        }

        /// <summary>
        /// Resolves the matching strategy using a custom resolver function.
        /// </summary>
        /// <param name="resolver">A function that resolves strategy IDs to strategy instances.</param>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when resolver is null.</exception>
        /// <example>
        /// <code>
        /// options.ResolveMatchingStrategy(id => MyStrategyFactory.Create(id));
        /// </code>
        /// </example>
        public XmlComparisonOptions ResolveMatchingStrategy(Func<string, IMatchingStrategy?> resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (!string.IsNullOrWhiteSpace(MatchingStrategyId))
            {
                MatchingStrategy = resolver(MatchingStrategyId);
            }
            return this;
        }

        /// <summary>
        /// Resolves the matching strategy from the <see cref="MatchingStrategyRegistry"/>.
        /// </summary>
        /// <returns>This options instance for fluent chaining.</returns>
        /// <example>
        /// <code>
        /// // Register the strategy first
        /// MatchingStrategyRegistry.Register("fuzzy", () => new FuzzyStrategy());
        ///
        /// // Then resolve from registry
        /// options.ResolveMatchingStrategyFromRegistry();
        /// </code>
        /// </example>
        public XmlComparisonOptions ResolveMatchingStrategyFromRegistry()
        {
            if (!string.IsNullOrWhiteSpace(MatchingStrategyId))
            {
                MatchingStrategy = MatchingStrategyRegistry.Resolve(MatchingStrategyId);
            }
            return this;
        }

        /// <summary>
        /// Loads options from a JSON file.
        /// </summary>
        /// <param name="path">The file path to load from.</param>
        /// <returns>A new <see cref="XmlComparisonOptions"/> instance with the loaded settings.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the JSON is invalid.</exception>
        /// <example>
        /// <code>
        /// var options = XmlComparisonOptions.LoadFromFile("comparison-settings.json");
        /// </code>
        /// </example>
        public static XmlComparisonOptions LoadFromFile(string path)
        {
            return FromJson(File.ReadAllText(path));
        }

        /// <summary>
        /// Builds and returns the underlying <see cref="XmlDiffConfig"/>.
        /// </summary>
        /// <returns>The configured <see cref="XmlDiffConfig"/> instance.</returns>
        /// <remarks>
        /// This method returns the Config property which contains all the comparison settings.
        /// </remarks>
        public XmlDiffConfig BuildConfig()
        {
            return Config;
        }

        /// <summary>
        /// Internal DTO for JSON serialization.
        /// </summary>
        private class XmlComparisonOptionsDto
        {
            public int Version { get; set; } = 1;
            public List<string>? ExcludedNodeNames { get; set; }
            public List<string>? ExcludedAttributeNames { get; set; }
            public List<string>? KeyAttributeNames { get; set; }
            public bool ExcludeSubtree { get; set; }
            public bool NormalizeWhitespace { get; set; }
            public bool TrimValues { get; set; }
            public bool NormalizeNewlines { get; set; }
            public bool IgnoreValues { get; set; }
            public List<string>? XsdPaths { get; set; }
            public bool GenerateHtml { get; set; }
            public bool GenerateJson { get; set; }
            public bool EmbedJsonInHtml { get; set; }
            public bool IncludeValidationInJson { get; set; }
            public string? MatchingStrategyId { get; set; }
        }
    }
}
