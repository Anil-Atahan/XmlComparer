using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Core engine for XML document comparison using configurable matching strategies.
    /// </summary>
    internal class XmlDiffEngine
    {
        private const int MaxChildrenPerNode = 10000;
        private readonly XmlDiffConfig _config;
        private readonly IMatchingStrategy _strategy;
        private readonly Dictionary<XElement, string> _pathCache;
        private readonly int _maxRecursionDepth;

        public XmlDiffEngine(XmlDiffConfig config, IMatchingStrategy strategy)
        {
            _config = config;
            _strategy = strategy;
            _pathCache = new Dictionary<XElement, string>();
            _maxRecursionDepth = XmlSecuritySettings.GetMaxRecursionDepth();
        }

        /// <summary>
        /// Compares two XML elements and returns the root of the diff tree.
        /// </summary>
        public DiffMatch Compare(XElement? originalRoot, XElement? newRoot)
        {
            if (originalRoot == null && newRoot == null) return new DiffMatch { Type = DiffType.Unchanged };

            var rootMatch = new DiffMatch
            {
                OriginalElement = originalRoot,
                NewElement = newRoot,
                Type = DiffType.Unchanged,
                Path = BuildPath(originalRoot ?? newRoot)
            };

            CompareChildrenRecursive(rootMatch, depth: 0);
            return rootMatch;
        }

        /// <summary>
        /// Recursively compares children of matched elements.
        /// </summary>
        private void CompareChildrenRecursive(DiffMatch match, int depth)
        {
            // Check recursion depth to prevent stack overflow
            if (depth > _maxRecursionDepth)
            {
                throw new InvalidOperationException(
                    $"Maximum recursion depth exceeded: {_maxRecursionDepth}. " +
                    "The XML document is too deeply nested. Consider flattening the structure or increasing the limit.");
            }

            if (match.OriginalElement == null && match.NewElement == null) return;
            bool isExcluded = IsExcluded(match.OriginalElement) || IsExcluded(match.NewElement);
            if (isExcluded && _config.ExcludeSubtree)
            {
                // Entire excluded subtree is ignored.
                match.Type = DiffType.Unchanged;
                return;
            }
            if (isExcluded && (match.OriginalElement == null || match.NewElement == null))
            {
                // Excluded node with no counterpart: suppress add/delete noise.
                match.Type = DiffType.Unchanged;
                return;
            }

            if (match.OriginalElement == null)
            {
                match.Type = DiffType.Added;

                var newChildren = match.NewElement!.Elements().ToList();
                CheckChildCount(newChildren.Count, match.NewElement);

                foreach (var child in newChildren)
                {
                    var childMatch = new DiffMatch { NewElement = child, Type = DiffType.Added, Path = BuildPath(child) };
                    match.Children.Add(childMatch);
                    CompareChildrenRecursive(childMatch, depth + 1);
                }
                return;
            }
            if (match.NewElement == null)
            {
                match.Type = DiffType.Deleted;

                var originalChildren = match.OriginalElement.Elements().ToList();
                CheckChildCount(originalChildren.Count, match.OriginalElement);

                foreach (var child in originalChildren)
                {
                    var childMatch = new DiffMatch { OriginalElement = child, Type = DiffType.Deleted, Path = BuildPath(child) };
                    match.Children.Add(childMatch);
                    CompareChildrenRecursive(childMatch, depth + 1);
                }
                return;
            }

            if (!isExcluded)
            {
                // For excluded nodes, skip node-level comparison but still compare children.
                bool isModified = CheckNodeModification(match);
                if (isModified) match.Type = DiffType.Modified;
            }

            var origChildren = match.OriginalElement.Elements().ToList();
            var newCh = match.NewElement.Elements().ToList();

            CheckChildCount(origChildren.Count, match.OriginalElement);
            CheckChildCount(newCh.Count, match.NewElement);

            var matches = MatchSiblings(origChildren, newCh);

            DetectMoves(matches);

            match.Children.AddRange(matches);

            foreach (var childMatch in matches)
            {
                CompareChildrenRecursive(childMatch, depth + 1);
            }

            if (match.Type == DiffType.Unchanged && match.Children.Any(c => c.Type != DiffType.Unchanged))
            {
                match.Type = DiffType.Modified;
            }
        }

        /// <summary>
        /// Validates child count to prevent DoS via excessive children.
        /// </summary>
        private void CheckChildCount(int count, XElement element)
        {
            if (count > MaxChildrenPerNode)
            {
                throw new InvalidOperationException(
                    $"Element '{element.Name.LocalName}' has {count} children, exceeding maximum of {MaxChildrenPerNode}. " +
                    "This may indicate a malformed document or DoS attempt.");
            }
        }

        private bool CheckNodeModification(DiffMatch match)
        {
            var e1 = match.OriginalElement!;
            var e2 = match.NewElement!;

            // Check for namespace changes if namespace comparison mode is not Ignore
            if (_config.NamespaceComparisonMode != NamespaceComparisonMode.Ignore)
            {
                if (XmlNamespaceHelper.HasNamespaceChanged(e1, e2, _config.NamespaceComparisonMode))
                {
                    match.Type = DiffType.NamespaceChanged;
                    match.Detail = XmlNamespaceHelper.GetNamespaceChangeDescription(e1, e2, _config.NamespaceComparisonMode);
                    return false; // Already marked as NamespaceChanged
                }

                // Track prefix changes if requested
                if (_config.TrackPrefixChanges && _config.NamespaceComparisonMode == NamespaceComparisonMode.UriSensitive)
                {
                    string prefix1 = XmlNamespaceHelper.GetNamespacePrefix(e1.Name);
                    string prefix2 = XmlNamespaceHelper.GetNamespacePrefix(e2.Name);
                    if (prefix1 != prefix2)
                    {
                        // Prefix changed but URI is the same - mark as modified
                        return true;
                    }
                }
            }

            // Use optimized attribute comparison without LINQ where possible
            var attrs1 = GetFilteredAttributes(e1);
            var attrs2 = GetFilteredAttributes(e2);

            if (attrs1.Count != attrs2.Count) return true;

            foreach (var kvp in attrs1)
            {
                if (!attrs2.TryGetValue(kvp.Key, out var val2) || val2 != kvp.Value) return true;
            }

            if (!e1.HasElements && !e2.HasElements && !_config.IgnoreValues)
            {
                var v1 = XmlValueNormalizer.Normalize(e1.Value, _config);
                var v2 = XmlValueNormalizer.Normalize(e2.Value, _config);
                if (v1 != v2) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets filtered attributes as a dictionary, using normalization.
        /// </summary>
        private Dictionary<XName, string> GetFilteredAttributes(XElement element)
        {
            var result = new Dictionary<XName, string>();
            foreach (var attr in element.Attributes())
            {
                // Skip namespace declaration attributes:
                // - xmlns:prefix="uri" (attr.Name.Namespace == XNamespace.Xmlns)
                // - xmlns="uri" (attr.Name.LocalName == "xmlns" with empty namespace)
                if (attr.Name.Namespace == XNamespace.Xmlns || attr.Name.LocalName == "xmlns")
                    continue;

                if (!_config.ExcludedAttributeNames.Contains(attr.Name.LocalName))
                {
                    result[attr.Name] = XmlValueNormalizer.Normalize(attr.Value, _config);
                }
            }
            return result;
        }

        private List<DiffMatch> MatchSiblings(List<XElement> originalList, List<XElement> newList)
        {
            if (_config.ExcludeSubtree)
            {
                originalList = originalList.Where(e => !IsExcluded(e)).ToList();
                newList = newList.Where(e => !IsExcluded(e)).ToList();
            }

            var results = new List<DiffMatch>();
            var usedNew = new HashSet<XElement>();

            // Build lookup for key attributes for O(1) access
            var keyAttributeLookup = BuildKeyAttributeLookup(newList);

            foreach (var original in originalList)
            {
                XElement? bestMatch = null;
                double bestScore = -1;

                // Try key attribute lookup first for O(1) match
                if (keyAttributeLookup.Count > 0)
                {
                    var keyValues = GetKeyAttributeValues(original);
                    if (keyValues.Count > 0)
                    {
                        var key = string.Join("|", keyValues);
                        if (keyAttributeLookup.TryGetValue(key, out var candidates))
                        {
                            foreach (var candidate in candidates)
                            {
                                if (usedNew.Contains(candidate)) continue;

                                double score = _strategy.Score(original, candidate, _config);
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestMatch = candidate;
                                }
                            }
                        }
                    }
                }

                // Fall back to O(n*m) search if no key match found
                if (bestMatch == null)
                {
                    foreach (var candidate in newList)
                    {
                        if (usedNew.Contains(candidate)) continue;

                        double score = _strategy.Score(original, candidate, _config);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestMatch = candidate;
                        }
                    }
                }

                if (bestMatch != null && bestScore > 0.5)
                {
                    results.Add(new DiffMatch
                    {
                        OriginalElement = original,
                        NewElement = bestMatch,
                        Type = DiffType.Unchanged,
                        Path = BuildPath(original)
                    });
                    usedNew.Add(bestMatch);
                }
                else
                {
                    results.Add(new DiffMatch
                    {
                        OriginalElement = original,
                        Type = DiffType.Deleted,
                        Path = BuildPath(original)
                    });
                }
            }

            foreach (var newItem in newList)
            {
                if (!usedNew.Contains(newItem))
                {
                    results.Add(new DiffMatch
                    {
                        NewElement = newItem,
                        Type = DiffType.Added,
                        Path = BuildPath(newItem)
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Builds a lookup dictionary for key attributes to optimize matching.
        /// </summary>
        private Dictionary<string, List<XElement>> BuildKeyAttributeLookup(List<XElement> elements)
        {
            var lookup = new Dictionary<string, List<XElement>>();

            if (_config.KeyAttributeNames.Count == 0) return lookup;

            foreach (var element in elements)
            {
                var keyValues = GetKeyAttributeValues(element);
                if (keyValues.Count > 0)
                {
                    var key = string.Join("|", keyValues);
                    if (!lookup.ContainsKey(key))
                    {
                        lookup[key] = new List<XElement>();
                    }
                    lookup[key].Add(element);
                }
            }

            return lookup;
        }

        /// <summary>
        /// Gets the values of key attributes for an element.
        /// </summary>
        private List<string> GetKeyAttributeValues(XElement element)
        {
            var values = new List<string>();
            foreach (var keyAttr in _config.KeyAttributeNames)
            {
                var attr = element.Attribute(keyAttr);
                if (attr != null)
                {
                    values.Add(attr.Value);
                }
            }
            return values;
        }

        private bool IsExcluded(XElement? element)
        {
            return element != null && _config.ExcludedNodeNames.Contains(element.Name.LocalName);
        }

        /// <summary>
        /// Builds an XPath-like path for an element with caching for performance.
        /// </summary>
        private string BuildPath(XElement? element)
        {
            if (element == null) return string.Empty;

            // Check cache first
            if (_pathCache.TryGetValue(element, out var cachedPath))
            {
                return cachedPath;
            }

            var parts = new Stack<string>();
            var current = element;
            while (current != null)
            {
                int index = 1;
                if (current.Parent != null)
                {
                    // Optimized index calculation without LINQ
                    int idx = 1;
                    foreach (var sibling in current.Parent.Elements(current.Name))
                    {
                        if (sibling == current) break;
                        idx++;
                    }
                    index = idx;
                }
                parts.Push($"{FormatName(current.Name)}[{index}]");
                current = current.Parent;
            }

            var path = "/" + string.Join("/", parts);

            // Cache the result
            _pathCache[element] = path;

            return path;
        }

        private string FormatName(XName name)
        {
            if (name.Namespace == XNamespace.None) return name.LocalName;
            return $"{{{name.NamespaceName}}}{name.LocalName}";
        }

        private void DetectMoves(List<DiffMatch> matches)
        {
            var matchedItems = matches.Where(m => m.OriginalElement != null && m.NewElement != null).ToList();
            if (matchedItems.Count < 2) return;

            var originalOrder = matchedItems.OrderBy(m => m.OriginalElement!.ElementsBeforeSelf().Count()).ToList();
            var newOrder = matchedItems.OrderBy(m => m.NewElement!.ElementsBeforeSelf().Count()).ToList();

            var lcs = LcsHelper.FindLcs(originalOrder, newOrder);
            var lcsSet = new HashSet<DiffMatch>(lcs);

            foreach (var match in matchedItems)
            {
                if (!lcsSet.Contains(match))
                {
                    match.Type = DiffType.Moved;
                }
            }
        }
    }
}
