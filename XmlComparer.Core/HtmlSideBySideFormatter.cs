using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Net;

namespace XmlComparer.Core
{
    /// <summary>
    /// Generates side-by-side HTML diff reports for XML comparison results.
    /// </summary>
    /// <remarks>
    /// <para>This class produces an interactive HTML report that displays the original and new XML
    /// documents side-by-side with color-coded differences. The output is a complete, self-contained
    /// HTML document with embedded CSS and JavaScript.</para>
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    ///   <item><description>Side-by-side comparison with line numbers</description></item>
    ///   <item><description>Color-coded changes (green for added, red for deleted, yellow for moved, blue for modified)</description></item>
    ///   <item><description>Inline word-level diffs for modified text values</description></item>
    ///   <item><description>Attribute highlighting with separate styles for added/removed/changed attributes</description></item>
    ///   <item><description>Toggle button to show/hide unchanged lines</description></item>
    ///   <item><description>Optional embedded JSON data for programmatic access</description></item>
    ///   <item><description>Validation results panel (if validation was performed)</description></item>
    ///   <item><description>Summary statistics panel showing counts of each change type</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var formatter = new HtmlSideBySideFormatter();
    ///
    /// // Basic HTML report
    /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
    /// string html = formatter.GenerateHtml(diff);
    /// File.WriteAllText("diff.html", html);
    ///
    /// // With embedded JSON
    /// var result = XmlComparer.CompareFilesWithReport("old.xml", "new.xml",
    ///     options => options.IncludeHtml());
    /// // result.Html contains the HTML with embedded JSON data
    /// </code>
    /// </example>
    public class HtmlSideBySideFormatter
    {
        /// <summary>
        /// Generates a side-by-side HTML diff report.
        /// </summary>
        /// <param name="root">The root of the diff tree.</param>
        /// <param name="embeddedJson">Optional JSON data to embed in the HTML for JavaScript access.</param>
        /// <returns>A complete HTML document as a string.</returns>
        /// <remarks>
        /// <para>The generated HTML includes embedded CSS for styling and JavaScript for interactivity.
        /// No external dependencies are required.</para>
        /// <para>When <paramref name="embeddedJson"/> is provided, it is embedded in a script tag
        /// with id "xml-diff-json" and can be accessed via <c>window.xmlDiff</c> in the browser.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var formatter = new HtmlSideBySideFormatter();
        /// var diff = XmlComparer.CompareFiles("old.xml", "new.xml");
        /// string html = formatter.GenerateHtml(diff);
        /// </code>
        /// </example>
        public string GenerateHtml(DiffMatch root, string? embeddedJson = null)
        {
            var sb = new StringBuilder();
            sb.Append(@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Consolas, 'Courier New', monospace; font-size: 12px; }
        table { width: 100%; border-collapse: collapse; table-layout: fixed; }
        td { vertical-align: top; padding: 2px 4px; border: 1px solid #ddd; white-space: pre-wrap; word-wrap: break-word; }
        .line-num { width: 40px; color: #999; text-align: right; user-select: none; background-color: #f5f5f5; }
        .code-cell { width: 45%; }

        .added-line { background-color: #e6ffec; }
        .deleted-line { background-color: #ffebe9; }
        .moved-line { background-color: #fffbdd; }
        .modified-line { background-color: #e8f0fe; }

        .inline-add { background-color: #abf2bc; font-weight: bold; }
        .inline-del { background-color: #ffc0c0; text-decoration: line-through; }

        .tag { color: #22863a; }
        .attr-name { color: #6f42c1; }
        .attr-val { color: #032f62; }
        .attr { padding: 0 1px; border-radius: 2px; }
        .attr-added { background-color: #c8f5d4; }
        .attr-removed { background-color: #ffd6d6; }
        .attr-changed { background-color: #ffe8b3; }
        .comment { color: #6a737d; }

        .controls { position: sticky; top: 0; background: #fff; padding: 10px; border-bottom: 1px solid #ccc; z-index: 100; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
        .btn { padding: 5px 10px; cursor: pointer; background-color: #0366d6; color: white; border: none; border-radius: 3px; font-size: 12px; }
        .btn:hover { background-color: #0255b3; }
        .hidden-row { display: none; }
        .hidden { display: none; }
        .validation { margin-top: 8px; font-size: 12px; }
        .validation-ok { color: #1a7f37; font-weight: bold; }
        .validation-fail { color: #d1242f; font-weight: bold; }
        .validation-list { margin: 6px 0 0 18px; padding: 0; }
        .summary { margin-top: 8px; font-size: 12px; }
        .summary-item { display: inline-block; margin-right: 10px; }
    </style>
    <script>
        function toggleUnchanged() {
            var rows = document.querySelectorAll('.tr-unchanged');
            var btn = document.getElementById('btnToggle');
            var isHidden = btn.getAttribute('data-hidden') === 'true';

            rows.forEach(function(row) {
                if (isHidden) {
                    row.classList.remove('hidden-row');
                } else {
                    row.classList.add('hidden-row');
                }
            });

            btn.setAttribute('data-hidden', !isHidden);
            btn.textContent = isHidden ? 'Hide Unchanged' : 'Show Unchanged';
        }
    </script>
</head>
<body>
    <div class=""controls"">
        <button id=""btnToggle"" class=""btn"" onclick=""toggleUnchanged()"" data-hidden=""false"">Hide Unchanged</button>
        <div id=""summary-panel"" class=""summary hidden""></div>
        <div id=""validation-panel"" class=""validation hidden"">
            <div id=""validation-summary""></div>
            <ul id=""validation-list"" class=""validation-list""></ul>
        </div>
    </div>
    <table>
");

            var rows = new List<DiffRow>();
            BuildRows(root, 0, rows);

            int leftLine = 1;
            int rightLine = 1;

            foreach (var row in rows)
            {
                string trClass = "tr-" + row.Type.ToString().ToLower();
                sb.Append($"<tr class='{trClass}'>");

                // Left Num
                sb.Append($"<td class='line-num'>{(row.LeftContent != null ? leftLine++.ToString() : "")}</td>");

                // Left Content
                string leftClass = GetRowClass(row.Type, true);
                sb.Append($"<td class='code-cell {leftClass}'>{row.LeftContent ?? ""}</td>");

                // Right Num
                sb.Append($"<td class='line-num'>{(row.RightContent != null ? rightLine++.ToString() : "")}</td>");

                // Right Content
                string rightClass = GetRowClass(row.Type, false);
                sb.Append($"<td class='code-cell {rightClass}'>{row.RightContent ?? ""}</td>");

                sb.Append("</tr>");
            }

            sb.Append(@"
    </table>
");

            if (!string.IsNullOrEmpty(embeddedJson))
            {
                // Escape only sequences that would terminate a script tag.
                string safeJson = EscapeForScriptTag(embeddedJson);
                sb.Append($"<script type=\"application/json\" id=\"xml-diff-json\">{safeJson}</script>");
                sb.Append(@"
<script>
    try {
        window.xmlDiff = JSON.parse(document.getElementById('xml-diff-json').textContent);
    } catch (e) {
        window.xmlDiff = null;
    }
    (function() {
        var summaryPanel = document.getElementById('summary-panel');
        var panel = document.getElementById('validation-panel');
        var summary = document.getElementById('validation-summary');
        var list = document.getElementById('validation-list');
        if (summaryPanel && window.xmlDiff && window.xmlDiff.Summary) {
            var s = window.xmlDiff.Summary;
            summaryPanel.classList.remove('hidden');
            summaryPanel.innerHTML =
                '<span class=""summary-item"">Total: ' + s.Total + '</span>' +
                '<span class=""summary-item"">Added: ' + s.Added + '</span>' +
                '<span class=""summary-item"">Deleted: ' + s.Deleted + '</span>' +
                '<span class=""summary-item"">Modified: ' + s.Modified + '</span>' +
                '<span class=""summary-item"">Moved: ' + s.Moved + '</span>' +
                '<span class=""summary-item"">Unchanged: ' + s.Unchanged + '</span>';
        }
        if (!panel || !summary || !list) return;
        if (!window.xmlDiff || !window.xmlDiff.Validation) {
            panel.classList.add('hidden');
            return;
        }
        var validation = window.xmlDiff.Validation;
        panel.classList.remove('hidden');
        list.innerHTML = '';
        if (validation.IsValid) {
            summary.textContent = 'Schema validation: OK';
            summary.classList.add('validation-ok');
        } else {
            summary.textContent = 'Schema validation: FAILED (' + validation.Errors.length + ')';
            summary.classList.add('validation-fail');
            validation.Errors.forEach(function(err) {
                var li = document.createElement('li');
                li.textContent = err.Message + ' (Line ' + err.LineNumber + ', Pos ' + err.LinePosition + ')';
                list.appendChild(li);
            });
        }
    })();
</script>");
            }

            sb.Append(@"
</body>
</html>");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the CSS class for a table row based on diff type and side.
        /// </summary>
        /// <param name="type">The diff type.</param>
        /// <param name="isLeft">True for the left (original) side, false for the right (new) side.</param>
        /// <returns>The CSS class name for styling the row.</returns>
        private string GetRowClass(DiffType type, bool isLeft)
        {
            if (type == DiffType.Added && !isLeft) return "added-line";
            if (type == DiffType.Deleted && isLeft) return "deleted-line";
            if (type == DiffType.Moved) return "moved-line";
            if (type == DiffType.Modified) return "modified-line";
            return "";
        }

        /// <summary>
        /// Represents a single row in the side-by-side HTML table.
        /// </summary>
        private class DiffRow
        {
            public string? LeftContent { get; set; }
            public string? RightContent { get; set; }
            public DiffType Type { get; set; }
        }

        /// <summary>
        /// Recursively builds table rows from the diff tree.
        /// </summary>
        /// <param name="match">The diff node to process.</param>
        /// <param name="indent">The indentation level (number of spaces = indent * 2).</param>
        /// <param name="rows">The output list to add rows to.</param>
        private void BuildRows(DiffMatch match, int indent, List<DiffRow> rows)
        {
            string indentStr = new string(' ', indent * 2);

            if (match.Type == DiffType.Added)
            {
                RenderBranch(match, indentStr, rows, false, DiffType.Added);
            }
            else if (match.Type == DiffType.Deleted)
            {
                RenderBranch(match, indentStr, rows, true, DiffType.Deleted);
            }
            else if (match.Type == DiffType.Moved)
            {
                // Moved is rendered as Deleted on Left and Added on Right (but colored Moved)
                // We can't easily align them side-by-side if they are moved, so we split them.
                // This logic is handled by the parent calling BuildRows usually, but if we are at root or recursive:
                // If we are here, it means we are processing a Moved node that was aligned?
                // No, AlignAndBuildRows handles the splitting.
                // If AlignAndBuildRows calls BuildRows for a Moved item, it means it found it in BOTH lists at the same position?
                // No, AlignAndBuildRows only calls BuildRows if `leftList[i] == rightList[j]`.
                // If they are the same object, it means they are "aligned" in the list processing.
                // But if they are marked Moved, it means they are NOT in the LCS.
                // So they shouldn't be aligned?
                // Wait, `DetectMoves` marks items as Moved if they are not in LCS.
                // But `MatchSiblings` returns them in a list.
                // `AlignAndBuildRows` iterates sorted lists.
                // If a Moved item happens to be at index 0 in both sorted lists (e.g. it was swapped with another),
                // `leftList[0] == rightList[0]` might be true?
                // Yes, if the sort order puts them there.
                // If they align in the sort order, we can display them side-by-side!
                // Even if they are "Moved" (out of LCS order relative to others), if they align locally here, why not show them side-by-side?
                // That would be nice.

                // So if we are here, we treat it like Modified/Unchanged but with Moved color.
                RenderAligned(match, indentStr, rows);
            }
            else // Modified or Unchanged
            {
                RenderAligned(match, indentStr, rows);
            }
        }

        /// <summary>
        /// Renders an aligned (present on both sides) diff node.
        /// </summary>
        /// <param name="match">The diff node to render.</param>
        /// <param name="indentStr">The indentation string.</param>
        /// <param name="rows">The output list to add rows to.</param>
        private void RenderAligned(DiffMatch match, string indentStr, List<DiffRow> rows)
        {
            string leftStart = FormatStartTag(match.OriginalElement, match.NewElement, true);
            string rightStart = FormatStartTag(match.NewElement, match.OriginalElement, false);

            rows.Add(new DiffRow
            {
                LeftContent = indentStr + leftStart,
                RightContent = indentStr + rightStart,
                Type = match.Type
            });

            if (!match.OriginalElement!.HasElements && !match.NewElement!.HasElements)
            {
                string val1 = match.OriginalElement.Value;
                string val2 = match.NewElement.Value;

                if (val1 != val2 && match.Type == DiffType.Modified)
                {
                    var diffs = TextDiffHelper.GetDiffs(val1, val2);
                    string leftHtml = "";
                    string rightHtml = "";

                    foreach(var d in diffs)
                    {
                        if (d.Type == DiffType.Deleted) leftHtml += $"<span class='inline-del'>{WebUtility.HtmlEncode(d.Token)}</span>";
                        else if (d.Type == DiffType.Added) rightHtml += $"<span class='inline-add'>{WebUtility.HtmlEncode(d.Token)}</span>";
                        else
                        {
                            leftHtml += WebUtility.HtmlEncode(d.Token);
                            rightHtml += WebUtility.HtmlEncode(d.Token);
                        }
                    }

                    rows.Add(new DiffRow
                    {
                        LeftContent = indentStr + "  " + leftHtml,
                        RightContent = indentStr + "  " + rightHtml,
                        Type = DiffType.Modified
                    });
                }
                else if (val1 != "" || val2 != "")
                {
                     rows.Add(new DiffRow
                    {
                        LeftContent = indentStr + "  " + WebUtility.HtmlEncode(val1),
                        RightContent = indentStr + "  " + WebUtility.HtmlEncode(val2),
                        Type = match.Type
                    });
                }
            }
            else
            {
                var leftList = match.Children.Where(c => c.OriginalElement != null)
                                             .OrderBy(c => c.OriginalElement!.ElementsBeforeSelf().Count())
                                             .ToList();
                var rightList = match.Children.Where(c => c.NewElement != null)
                                              .OrderBy(c => c.NewElement!.ElementsBeforeSelf().Count())
                                              .ToList();
                AlignAndBuildRows(leftList, rightList, indentStr.Length / 2 + 1, rows);
            }

            rows.Add(new DiffRow
            {
                LeftContent = indentStr + $"&lt;/{match.OriginalElement!.Name.LocalName}&gt;",
                RightContent = indentStr + $"&lt;/{match.NewElement!.Name.LocalName}&gt;",
                Type = match.Type
            });
        }

        /// <summary>
        /// Aligns children from both sides and builds rows.
        /// </summary>
        /// <param name="leftList">The left-side child list.</param>
        /// <param name="rightList">The right-side child list.</param>
        /// <param name="indent">The indentation level.</param>
        /// <param name="rows">The output list to add rows to.</param>
        private void AlignAndBuildRows(List<DiffMatch> leftList, List<DiffMatch> rightList, int indent, List<DiffRow> rows)
        {
            int i = 0;
            int j = 0;
            string indentStr = new string(' ', indent * 2);

            while (i < leftList.Count || j < rightList.Count)
            {
                if (i < leftList.Count && j < rightList.Count && leftList[i] == rightList[j])
                {
                    BuildRows(leftList[i], indent, rows);
                    i++;
                    j++;
                }
                else
                {
                    bool isLeftDeleted = i < leftList.Count && leftList[i].Type == DiffType.Deleted;
                    bool isRightAdded = j < rightList.Count && rightList[j].Type == DiffType.Added;

                    if (isLeftDeleted)
                    {
                        BuildRows(leftList[i], indent, rows);
                        i++;
                    }
                    else if (isRightAdded)
                    {
                        BuildRows(rightList[j], indent, rows);
                        j++;
                    }
                    else
                    {
                        // Moved or Reordered
                        if (i < leftList.Count)
                        {
                             RenderBranch(leftList[i], indentStr, rows, true, DiffType.Moved);
                             i++;
                        }
                        else if (j < rightList.Count)
                        {
                             RenderBranch(rightList[j], indentStr, rows, false, DiffType.Moved);
                             j++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Renders a branch (added, deleted, or moved subtree) on one side only.
        /// </summary>
        /// <param name="match">The diff node to render.</param>
        /// <param name="indent">The indentation string.</param>
        /// <param name="rows">The output list to add rows to.</param>
        /// <param name="isLeft">True for left side, false for right side.</param>
        /// <param name="overrideType">The diff type to use for styling.</param>
        private void RenderBranch(DiffMatch match, string indent, List<DiffRow> rows, bool isLeft, DiffType overrideType)
        {
            var e = isLeft ? match.OriginalElement : match.NewElement;
            if (e == null) return;

            string start = FormatStartTag(e, null, isLeft);

            rows.Add(new DiffRow
            {
                LeftContent = isLeft ? indent + start : null,
                RightContent = !isLeft ? indent + start : null,
                Type = overrideType
            });

            var children = match.Children.Where(c => isLeft ? c.OriginalElement != null : c.NewElement != null)
                                         .OrderBy(c => isLeft ? c.OriginalElement!.ElementsBeforeSelf().Count() : c.NewElement!.ElementsBeforeSelf().Count())
                                         .ToList();

            foreach(var child in children)
            {
                RenderBranch(child, indent + "  ", rows, isLeft, overrideType);
            }

            if (!e.HasElements && !string.IsNullOrEmpty(e.Value))
            {
                 rows.Add(new DiffRow
                {
                    LeftContent = isLeft ? indent + "  " + WebUtility.HtmlEncode(e.Value) : null,
                    RightContent = !isLeft ? indent + "  " + WebUtility.HtmlEncode(e.Value) : null,
                    Type = overrideType
                });
            }

            rows.Add(new DiffRow
            {
                LeftContent = isLeft ? indent + $"&lt;/{e.Name.LocalName}&gt;" : null,
                RightContent = !isLeft ? indent + $"&lt;/{e.Name.LocalName}&gt;" : null,
                Type = overrideType
            });
        }

        /// <summary>
        /// Formats an element's start tag with syntax highlighting.
        /// </summary>
        /// <param name="e">The element to format.</param>
        /// <param name="other">The corresponding element from the other side for attribute comparison.</param>
        /// <param name="isLeft">True if formatting for the left (original) side.</param>
        /// <returns>HTML string with syntax highlighting.</returns>
        private string FormatStartTag(XElement? e, XElement? other, bool isLeft)
        {
            if (e == null) return "";
            var sb = new StringBuilder();
            sb.Append($"&lt;<span class='tag'>{WebUtility.HtmlEncode(e.Name.LocalName)}</span>");
            foreach(var attr in e.Attributes())
            {
                string cssClass = GetAttributeClass(attr, other, isLeft);
                sb.Append($" <span class='attr {cssClass}'><span class='attr-name'>{WebUtility.HtmlEncode(attr.Name.LocalName)}</span>=\"<span class='attr-val'>{WebUtility.HtmlEncode(attr.Value)}</span>\"</span>");
            }
            sb.Append("&gt;");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the CSS class for an attribute based on how it changed.
        /// </summary>
        /// <param name="attr">The attribute to check.</param>
        /// <param name="other">The corresponding element from the other side.</param>
        /// <param name="isLeft">True if checking for the left (original) side.</param>
        /// <returns>The CSS class for the attribute.</returns>
        private string GetAttributeClass(XAttribute attr, XElement? other, bool isLeft)
        {
            if (other == null) return "";

            var otherAttr = other.Attribute(attr.Name);
            if (otherAttr == null) return isLeft ? "attr-removed" : "attr-added";
            if (otherAttr.Value != attr.Value) return "attr-changed";
            return "";
        }

        /// <summary>
        /// Escapes JSON content for safe embedding in a script tag.
        /// </summary>
        /// <param name="json">The JSON string to escape.</param>
        /// <returns>The escaped JSON string.</returns>
        /// <remarks>
        /// Only escapes sequences that would terminate a script tag (specifically "&lt;/").
        /// </remarks>
        private static string EscapeForScriptTag(string json)
        {
            return json.Replace("</", "<\\/");
        }
    }
}
