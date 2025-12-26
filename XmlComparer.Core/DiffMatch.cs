using System.Collections.Generic;
using System.Xml.Linq;

namespace XmlComparer.Core
{
    /// <summary>
    /// Represents the type of XML node represented by a <see cref="DiffMatch"/>.
    /// </summary>
    /// <remarks>
    /// When node preservation is enabled via <see cref="XmlDiffConfig.NodePreservation"/>,
    /// <see cref="DiffMatch"/> nodes can represent non-element XML nodes such as comments,
    /// CDATA sections, and processing instructions.
    /// </remarks>
    public enum DiffNodeType
    {
        /// <summary>
        /// The node is an XML element (the default).
        /// </summary>
        Element,

        /// <summary>
        /// The node is an XML comment (&lt;!-- --&gt;).
        /// </summary>
        Comment,

        /// <summary>
        /// The node is a CDATA section (&lt;![CDATA[ ]]&gt;).
        /// </summary>
        CData,

        /// <summary>
        /// The node is a processing instruction (&lt;?target ?&gt;).
        /// </summary>
        ProcessingInstruction
    }

    /// <summary>
    /// Represents a node in the hierarchical diff tree, containing information about
    /// differences between corresponding XML elements or nodes.
    /// </summary>
    /// <remarks>
    /// <para>The diff tree structure mirrors the XML document structure:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="DiffMatch.Type"/> indicates what kind of change occurred</description></item>
    ///   <item><description><see cref="DiffMatch.NodeType"/> indicates the type of XML node (Element, Comment, CDATA, PI)</description></item>
    ///   <item><description><see cref="DiffMatch.Children"/> contains recursive diffs for child elements</description></item>
    ///   <item><description><see cref="DiffMatch.Path"/> provides an XPath-like location in the document</description></item>
    /// </list>
    /// <para>Traverse the tree recursively to find all changes:</para>
    /// <code>
    /// void PrintChanges(DiffMatch diff, int indent = 0)
    /// {
    ///     if (diff.Type != DiffType.Unchanged)
    ///     {
    ///         Console.WriteLine($"{new string(' ', indent)}{diff.Type}: {diff.Path}");
    ///     }
    ///     foreach (var child in diff.Children)
    ///         PrintChanges(child, indent + 2);
    /// }
    /// </code>
    /// </remarks>
    public class DiffMatch
    {
        /// <summary>
        /// Gets or sets the element from the original XML document.
        /// Null if the element was added in the new document.
        /// </summary>
        public XElement? OriginalElement { get; set; }

        /// <summary>
        /// Gets or sets the element from the new XML document.
        /// Null if the element was deleted from the original document.
        /// </summary>
        public XElement? NewElement { get; set; }

        /// <summary>
        /// Gets or sets the type of change detected for this node.
        /// </summary>
        public DiffType Type { get; set; }

        /// <summary>
        /// Gets or sets the type of XML node represented by this diff.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="XmlDiffConfig.NodePreservation"/> is enabled, this property
        /// indicates whether the node is an Element, Comment, CDATA section, or Processing Instruction.</para>
        /// <para>The default value is <see cref="DiffNodeType.Element"/>.</para>
        /// </remarks>
        public DiffNodeType NodeType { get; set; } = DiffNodeType.Element;

        /// <summary>
        /// Gets or sets the XPath-like path to this element in the document.
        /// </summary>
        /// <remarks>
        /// Format: <c>/root[1]/child[2]/item[1]</c>
        /// <para>Namespace format: <c>/{namespace}element[index]</c></para>
        /// <para>For non-element nodes: <c>/root[1]/comment()[1]</c>, <c>/root[1]/text()[1]</c></para>
        /// </remarks>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a human-readable description of the change.
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of a comment node (when <see cref="NodeType"/> is <see cref="DiffNodeType.Comment"/>).
        /// </summary>
        /// <remarks>
        /// This property contains the text content of XML comments, excluding the &lt;!-- and --&gt; delimiters.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;!-- This is a comment --&gt;
        /// // CommentContent = "This is a comment"
        /// </code>
        /// </example>
        public string? CommentContent { get; set; }

        /// <summary>
        /// Gets or sets the content of a CDATA node (when <see cref="NodeType"/> is <see cref="DiffNodeType.CData"/>).
        /// </summary>
        /// <remarks>
        /// This property contains the text content of CDATA sections, excluding the &lt;![CDATA[ and ]]&gt; delimiters.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;![CDATA[Some text data]]&gt;
        /// // CDataContent = "Some text data"
        /// </code>
        /// </example>
        public string? CDataContent { get; set; }

        /// <summary>
        /// Gets or sets the target of a processing instruction (when <see cref="NodeType"/> is <see cref="DiffNodeType.ProcessingInstruction"/>).
        /// </summary>
        /// <remarks>
        /// The target identifies the application to which the processing instruction is directed.
        /// Common targets include "xml-stylesheet", "xml-model", and "php".
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;?xml-stylesheet href="style.css" type="text/css"?&gt;
        /// // PITarget = "xml-stylesheet"
        /// </code>
        /// </example>
        public string? PITarget { get; set; }

        /// <summary>
        /// Gets or sets the data of a processing instruction (when <see cref="NodeType"/> is <see cref="DiffNodeType.ProcessingInstruction"/>).
        /// </summary>
        /// <remarks>
        /// This property contains the data portion of the processing instruction, excluding the target and the &lt;? and ?&gt; delimiters.
        /// </remarks>
        /// <example>
        /// <code>
        /// &lt;?xml-stylesheet href="style.css" type="text/css"?&gt;
        /// // PIData = "href=\"style.css\" type=\"text/css\""
        /// </code>
        /// </example>
        public string? PIData { get; set; }

        /// <summary>
        /// Gets or sets the child element diffs, forming a recursive tree structure.
        /// </summary>
        /// <remarks>
        /// This collection contains the diffs for all child elements of the current node.
        /// Traverse recursively to examine all changes in the document.
        /// </remarks>
        public List<DiffMatch> Children { get; set; } = new List<DiffMatch>();
    }
}
