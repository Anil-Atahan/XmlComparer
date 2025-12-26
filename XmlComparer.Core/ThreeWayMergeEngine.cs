using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

namespace XmlComparer.Core
{
    /// <summary>
    /// Performs three-way merge operations on XML documents.
    /// </summary>
    /// <remarks>
    /// <para>Three-way merge is a version control technique that combines changes from two branches
    /// using a common ancestor (base) as reference. This allows automatic merging when changes
    /// don't overlap, and clear conflict identification when they do.</para>
    /// <para><b>The Merge Process:</b></para>
    /// <list type="number">
    ///   <item><description>Compare base to ours to identify "our" changes</description></item>
    ///   <item><description>Compare base to theirs to identify "their" changes</description></item>
    ///   <item><description>Merge changes where they don't conflict</description></item>
    ///   <item><description>Create conflict markers for overlapping changes</description></item>
    ///   <item><description>Apply conflict resolver if configured</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var engine = new ThreeWayMergeEngine();
    /// var config = new XmlDiffConfig
    /// {
    ///     KeyAttributeNames = { "id" }
    /// };
    ///
    /// // Synchronous merge
    /// var result = engine.Merge(baseXml, oursXml, theirsXml, config);
    ///
    /// // Asynchronous merge
    /// var result = await engine.MergeAsync(baseXml, oursXml, theirsXml, config);
    ///
    /// // With custom conflict resolver
    /// var result = engine.Merge(baseXml, oursXml, theirsXml, config,
    ///     new MergeConflictResolvers.OursResolver());
    /// </code>
    /// </example>
    public class ThreeWayMergeEngine
    {
        private readonly XmlComparerService _comparer;
        private readonly XmlDiffEngine _engine;

        /// <summary>
        /// Creates a new ThreeWayMergeEngine with default settings.
        /// </summary>
        public ThreeWayMergeEngine() : this(new XmlComparerService(new XmlDiffConfig())) { }

        /// <summary>
        /// Creates a new ThreeWayMergeEngine with the specified comparer.
        /// </summary>
        /// <param name="comparer">The XML comparer service to use for detecting changes.</param>
        public ThreeWayMergeEngine(XmlComparerService comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            _engine = new XmlDiffEngine(new XmlDiffConfig(), new DefaultMatchingStrategy());
        }

        /// <summary>
        /// Performs a three-way merge synchronously.
        /// </summary>
        /// <param name="baseXml">The base (ancestor) XML document.</param>
        /// <param name="oursXml">The "ours" branch XML document.</param>
        /// <param name="theirsXml">The "theirs" branch XML document.</param>
        /// <param name="config">The comparison configuration.</param>
        /// <param name="conflictResolver">Optional conflict resolver.</param>
        /// <returns>A merge result containing the merged document and any conflicts.</returns>
        public MergeResult Merge(
            string baseXml,
            string oursXml,
            string theirsXml,
            XmlDiffConfig? config = null,
            IMergeConflictResolver? conflictResolver = null)
        {
            try
            {
                // Parse documents
                var baseDoc = XDocument.Parse(baseXml);
                var oursDoc = XDocument.Parse(oursXml);
                var theirsDoc = XDocument.Parse(theirsXml);

                return MergeDocuments(baseDoc, oursDoc, theirsDoc, config, conflictResolver);
            }
            catch (Exception ex)
            {
                return MergeResult.Failure("Failed to parse XML documents", ex);
            }
        }

        /// <summary>
        /// Performs a three-way merge asynchronously.
        /// </summary>
        /// <param name="baseXml">The base (ancestor) XML document.</param>
        /// <param name="oursXml">The "ours" branch XML document.</param>
        /// <param name="theirsXml">The "theirs" branch XML document.</param>
        /// <param name="config">The comparison configuration.</param>
        /// <param name="conflictResolver">Optional conflict resolver.</param>
        /// <returns>A task representing the merge operation.</returns>
        public Task<MergeResult> MergeAsync(
            string baseXml,
            string oursXml,
            string theirsXml,
            XmlDiffConfig? config = null,
            IMergeConflictResolver? conflictResolver = null)
        {
            return Task.FromResult(Merge(baseXml, oursXml, theirsXml, config, conflictResolver));
        }

        /// <summary>
        /// Performs a three-way merge on XDocuments.
        /// </summary>
        /// <param name="baseDoc">The base (ancestor) XML document.</param>
        /// <param name="oursDoc">The "ours" branch XML document.</param>
        /// <param name="theirsDoc">The "theirs" branch XML document.</param>
        /// <param name="config">The comparison configuration.</param>
        /// <param name="conflictResolver">Optional conflict resolver.</param>
        /// <returns>A merge result containing the merged document and any conflicts.</returns>
        public MergeResult MergeDocuments(
            XDocument baseDoc,
            XDocument oursDoc,
            XDocument theirsDoc,
            XmlDiffConfig? config = null,
            IMergeConflictResolver? conflictResolver = null)
        {
            try
            {
                config ??= new XmlDiffConfig();

                // Compare base -> ours to get our changes
                var oursDiff = _engine.Compare(baseDoc.Root, oursDoc.Root);

                // Compare base -> theirs to get their changes
                var theirsDiff = _engine.Compare(baseDoc.Root, theirsDoc.Root);

                // Perform the merge
                var stats = new MergeStatistics();
                var conflicts = new List<MergeConflict>();

                // Start with a copy of ours (we'll merge theirs into it)
                var merged = new XDocument(oursDoc);
                var mergedRoot = merged.Root;

                if (mergedRoot == null)
                {
                    return MergeResult.Failure("Ours document has no root element");
                }

                // Process the merge
                MergeRecursive(
                    baseDoc.Root,
                    oursDoc.Root,
                    theirsDoc.Root,
                    mergedRoot,
                    oursDiff,
                    theirsDiff,
                    "",
                    stats,
                    conflicts,
                    config,
                    conflictResolver);

                // Resolve conflicts if resolver is provided
                if (conflictResolver != null && conflicts.Any())
                {
                    ResolveConflicts(mergedRoot, conflicts, conflictResolver, stats);
                }

                // Update conflict count in stats
                stats.ConflictCount = conflicts.Count;

                // Create result
                var result = conflicts.Any()
                    ? MergeResult.WithConflicts(merged, conflicts, stats)
                    : MergeResult.Success(merged, stats);

                return result;
            }
            catch (Exception ex)
            {
                return MergeResult.Failure("Merge failed", ex);
            }
        }

        /// <summary>
        /// Recursively merges XML elements.
        /// </summary>
        private void MergeRecursive(
            XElement? baseElement,
            XElement? oursElement,
            XElement? theirsElement,
            XElement mergedElement,
            DiffMatch oursDiff,
            DiffMatch theirsDiff,
            string path,
            MergeStatistics stats,
            List<MergeConflict> conflicts,
            XmlDiffConfig config,
            IMergeConflictResolver? conflictResolver)
        {
            stats.TotalElements++;

            // Get children from all three versions
            var baseChildren = baseElement?.Elements() ?? Enumerable.Empty<XElement>();
            var oursChildren = oursElement?.Elements() ?? Enumerable.Empty<XElement>();
            var theirsChildren = theirsElement?.Elements() ?? Enumerable.Empty<XElement>();

            // Build maps for key-based matching if key attributes are configured
            var hasKeyAttributes = config.KeyAttributeNames.Count > 0;
            var baseMap = BuildKeyMap(baseChildren, config);
            var oursMap = BuildKeyMap(oursChildren, config);
            var theirsMap = BuildKeyMap(theirsChildren, config);

            // Track processed keys
            var processedKeys = new HashSet<string>();
            var processedOursIndices = new HashSet<int>();
            var processedTheirsIndices = new HashSet<int>();

            // Process elements by key if configured
            if (hasKeyAttributes)
            {
                // Get all unique keys from all branches
                var allKeys = baseMap.Keys
                    .Concat(oursMap.Keys)
                    .Concat(theirsMap.Keys)
                    .Distinct();

                foreach (var key in allKeys)
                {
                    string childPath = string.IsNullOrEmpty(path)
                        ? $"/{key}"
                        : $"{path}/{key}";

                    baseMap.TryGetValue(key, out var baseChild);
                    oursMap.TryGetValue(key, out var oursChild);
                    theirsMap.TryGetValue(key, out var theirsChild);

                    // Check for conflicts
                    var conflict = DetectConflict(baseChild, oursChild, theirsChild, childPath);
                    if (conflict != null)
                    {
                        conflicts.Add(conflict);
                        stats.ConflictCount++;

                        // Add placeholder for conflict resolution
                        if (conflictResolver != null)
                        {
                            var resolved = conflictResolver.Resolve(conflict);
                            if (resolved != null)
                            {
                                mergedElement.Add(resolved);
                                stats.ResolverResolvedConflicts++;
                            }
                        }
                        else
                        {
                            // Add conflict marker
                            AddConflictMarker(mergedElement, conflict);
                        }
                    }
                    else
                    {
                        // No conflict - merge the elements
                        var mergedChild = MergeElement(baseChild, oursChild, theirsChild, stats);
                        if (mergedChild != null)
                        {
                            mergedElement.Add(mergedChild);

                            // Recursively merge children
                            MergeRecursive(
                                baseChild,
                                oursChild,
                                theirsChild,
                                mergedChild,
                                oursDiff,
                                theirsDiff,
                                childPath,
                                stats,
                                conflicts,
                                config,
                                conflictResolver);
                        }
                    }

                    processedKeys.Add(key);
                }
            }
            else
            {
                // Process by position (no key attributes)
                int maxLength = Math.Max(
                    Math.Max(baseChildren.Count(), oursChildren.Count()),
                    theirsChildren.Count()
                );

                for (int i = 0; i < maxLength; i++)
                {
                    var baseChild = baseChildren.ElementAtOrDefault(i);
                    var oursChild = oursChildren.ElementAtOrDefault(i);
                    var theirsChild = theirsChildren.ElementAtOrDefault(i);

                    string childName = oursChild?.Name.LocalName
                        ?? theirsChild?.Name.LocalName
                        ?? baseChild?.Name.LocalName
                        ?? "element";

                    string childPath = string.IsNullOrEmpty(path)
                        ? $"/{childName}[{i}]"
                        : $"{path}/{childName}[{i}]";

                    var conflict = DetectConflict(baseChild, oursChild, theirsChild, childPath);
                    if (conflict != null)
                    {
                        conflicts.Add(conflict);
                        stats.ConflictCount++;

                        if (conflictResolver != null)
                        {
                            var resolved = conflictResolver.Resolve(conflict);
                            if (resolved != null)
                            {
                                mergedElement.Add(resolved);
                                stats.ResolverResolvedConflicts++;
                            }
                        }
                        else
                        {
                            AddConflictMarker(mergedElement, conflict);
                        }
                    }
                    else
                    {
                        var mergedChild = MergeElement(baseChild, oursChild, theirsChild, stats);
                        if (mergedChild != null)
                        {
                            mergedElement.Add(mergedChild);

                            MergeRecursive(
                                baseChild,
                                oursChild,
                                theirsChild,
                                mergedChild,
                                oursDiff,
                                theirsDiff,
                                childPath,
                                stats,
                                conflicts,
                                config,
                                conflictResolver);
                        }
                    }
                }
            }

            stats.UnchangedElements++;
        }

        /// <summary>
        /// Detects if there's a conflict between the three versions.
        /// </summary>
        private MergeConflict? DetectConflict(
            XElement? baseElement,
            XElement? oursElement,
            XElement? theirsElement,
            string path)
        {
            // Both added different elements
            if (baseElement == null && oursElement != null && theirsElement != null)
            {
                if (!ElementsEqual(oursElement, theirsElement))
                {
                    return new MergeConflict
                    {
                        Path = path,
                        BaseElement = baseElement,
                        OursElement = oursElement,
                        TheirsElement = theirsElement,
                        ConflictType = MergeConflictType.AddAdd
                    };
                }
            }

            // Modify-delete conflict
            if (baseElement != null)
            {
                if (oursElement == null && theirsElement != null)
                {
                    return new MergeConflict
                    {
                        Path = path,
                        BaseElement = baseElement,
                        OursElement = oursElement,
                        TheirsElement = theirsElement,
                        ConflictType = MergeConflictType.ModifyDelete
                    };
                }
                if (oursElement != null && theirsElement == null)
                {
                    return new MergeConflict
                    {
                        Path = path,
                        BaseElement = baseElement,
                        OursElement = oursElement,
                        TheirsElement = theirsElement,
                        ConflictType = MergeConflictType.ModifyDelete
                    };
                }
            }

            // Modify-modify conflict
            if (baseElement != null && oursElement != null && theirsElement != null)
            {
                if (!ElementsEqual(oursElement, theirsElement) &&
                    !ElementsEqual(oursElement, baseElement) &&
                    !ElementsEqual(theirsElement, baseElement))
                {
                    return new MergeConflict
                    {
                        Path = path,
                        BaseElement = baseElement,
                        OursElement = oursElement,
                        TheirsElement = theirsElement,
                        ConflictType = MergeConflictType.ModifyModify
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Merges a single element from the three versions.
        /// </summary>
        private XElement? MergeElement(
            XElement? baseElement,
            XElement? oursElement,
            XElement? theirsElement,
            MergeStatistics stats)
        {
            // If ours is null but theirs isn't, this is a deletion in ours
            // We'll keep theirs (it will be handled by conflict detection if conflicting)
            if (oursElement == null && theirsElement != null)
            {
                stats.TheirsOnlyChanges++;
                return new XElement(theirsElement);
            }

            // If theirs is null but ours isn't, keep ours
            if (theirsElement == null && oursElement != null)
            {
                stats.OursOnlyChanges++;
                return new XElement(oursElement);
            }

            // If both are null, return null (deleted in both)
            if (oursElement == null && theirsElement == null)
            {
                return null;
            }

            // At this point, both ours and theirs exist
            // If they're equal, just use one
            if (ElementsEqual(oursElement!, theirsElement!))
            {
                stats.AutoMergedChanges++;
                return new XElement(oursElement!);
            }

            // Use ours as the base and merge attributes from theirs
            var merged = new XElement(oursElement!);

            // Merge attributes - add any unique attributes from theirs
            foreach (var attr in theirsElement!.Attributes())
            {
                var oursAttr = oursElement!.Attribute(attr.Name);
                if (oursAttr == null)
                {
                    merged.SetAttributeValue(attr.Name, attr.Value);
                }
            }

            stats.AutoMergedChanges++;
            return merged;
        }

        /// <summary>
        /// Adds a conflict marker element to the merged document.
        /// </summary>
        private void AddConflictMarker(XElement parent, MergeConflict conflict)
        {
            var marker = new XElement("MergeConflict",
                new XAttribute("path", conflict.Path),
                new XAttribute("type", conflict.ConflictType.ToString()));

            if (conflict.BaseElement != null)
            {
                marker.Add(new XElement("Base", conflict.BaseElement));
            }
            if (conflict.OursElement != null)
            {
                marker.Add(new XElement("Ours", conflict.OursElement));
            }
            if (conflict.TheirsElement != null)
            {
                marker.Add(new XElement("Theirs", conflict.TheirsElement));
            }

            parent.Add(marker);
        }

        /// <summary>
        /// Resolves conflicts using the provided resolver.
        /// </summary>
        private void ResolveConflicts(
            XElement root,
            List<MergeConflict> conflicts,
            IMergeConflictResolver resolver,
            MergeStatistics stats)
        {
            // Remove conflict markers and replace with resolved elements
            var conflictMarkers = root.Descendants("MergeConflict").ToList();

            foreach (var marker in conflictMarkers)
            {
                var path = marker.Attribute("path")?.Value;
                var conflict = conflicts.FirstOrDefault(c => c.Path == path);

                if (conflict != null)
                {
                    var resolved = resolver.Resolve(conflict);
                    if (resolved != null)
                    {
                        marker.ReplaceWith(resolved);
                        stats.ResolverResolvedConflicts++;
                    }
                    else
                    {
                        marker.Remove();
                    }
                }
            }
        }

        /// <summary>
        /// Builds a map of element keys to elements for key-based matching.
        /// </summary>
        private Dictionary<string, XElement> BuildKeyMap(IEnumerable<XElement> elements, XmlDiffConfig config)
        {
            var map = new Dictionary<string, XElement>();

            foreach (var element in elements)
            {
                var key = BuildElementKey(element, config);
                if (!string.IsNullOrEmpty(key))
                {
                    map[key] = element;
                }
            }

            return map;
        }

        /// <summary>
        /// Builds a key for an element based on configured key attributes.
        /// </summary>
        private string BuildElementKey(XElement element, XmlDiffConfig config)
        {
            if (config.KeyAttributeNames.Count == 0)
                return element.Name.LocalName;

            var keyParts = new List<string> { element.Name.LocalName };

            foreach (var attrName in config.KeyAttributeNames)
            {
                var attr = element.Attribute(attrName);
                if (attr != null)
                {
                    keyParts.Add(attr.Value);
                }
            }

            return string.Join(":", keyParts);
        }

        /// <summary>
        /// Compares two elements for equality (name, attributes, and value).
        /// </summary>
        private bool ElementsEqual(XElement? a, XElement? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            if (a.Name != b.Name) return false;
            if (a.Value != b.Value) return false;

            var aAttrs = a.Attributes().OrderBy(x => x.Name.ToString()).ToList();
            var bAttrs = b.Attributes().OrderBy(x => x.Name.ToString()).ToList();

            if (aAttrs.Count != bAttrs.Count) return false;

            for (int i = 0; i < aAttrs.Count; i++)
            {
                if (aAttrs[i].Name != bAttrs[i].Name) return false;
                if (aAttrs[i].Value != bAttrs[i].Value) return false;
            }

            return true;
        }
    }
}
