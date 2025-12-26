using System.Collections.Generic;

namespace XmlComparer.Core
{
    /// <summary>
    /// Configuration for preserving and comparing non-element XML nodes.
    /// </summary>
    /// <remarks>
    /// <para>By default, the XML comparison engine strips CDATA sections, comments, and processing instructions
    /// during document loading to focus on element and attribute differences. This class allows you to
    /// configure which non-element nodes should be preserved and compared.</para>
    /// <para><b>Supported Node Types:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>CDATA sections</b> - Text data that should not be parsed by the XML parser</description></item>
    ///   <item><description><b>Comments</b> - XML comments (&lt;!-- --&gt;) for documentation</description></item>
    ///   <item><description><b>Processing Instructions</b> - Targeted instructions to applications (&lt;?target ?&gt;)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var settings = new XmlNodePreservationSettings
    /// {
    ///     Mode = XmlNodePreservationMode.PreserveAll,
    ///     TrackCommentPosition = true,
    ///     NormalizeCDataWhitespace = true,
    ///     PreservePITargets = new List&lt;string&gt; { "xml-stylesheet", "xml-model" }
    /// };
    /// </code>
    /// </example>
    public class XmlNodePreservationSettings
    {
        /// <summary>
        /// Gets or sets which non-element nodes to preserve during comparison.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="XmlNodePreservationMode.None"/>, which strips all non-element nodes
        /// for backward compatibility.
        /// </remarks>
        /// <example>
        /// <code>
        /// settings.Mode = XmlNodePreservationMode.PreserveAll;
        /// </code>
        /// </example>
        public XmlNodePreservationMode Mode { get; set; } = XmlNodePreservationMode.None;

        /// <summary>
        /// Gets or sets whether to track the exact position of comments within their parent elements.
        /// </summary>
        /// <remarks>
        /// When true, comments that appear at different positions within their parent element
        /// will be flagged as Modified. When false, only the presence/absence and content of comments
        /// is compared, regardless of position.
        /// </remarks>
        /// <example>
        /// <code>
        /// settings.TrackCommentPosition = true;
        /// </code>
        /// </example>
        public bool TrackCommentPosition { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to normalize whitespace in CDATA sections during comparison.
        /// </summary>
        /// <remarks>
        /// When true, CDATA content is normalized using the same rules as text content
        /// (whitespace, newline, trimming settings from <see cref="XmlDiffConfig"/>).
        /// This is useful when comparing XML where formatting may differ.
        /// </remarks>
        /// <example>
        /// <code>
        /// settings.NormalizeCDataWhitespace = true;
        /// </code>
        /// </example>
        public bool NormalizeCDataWhitespace { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of processing instruction targets to preserve.
        /// </summary>
        /// <remarks>
        /// <para>When specified, only processing instructions with these targets are preserved.
        /// Common targets include:</para>
        /// <list type="bullet">
        ///   <item><description><c>xml-stylesheet</c> - Associates a stylesheet with the document</description></item>
        ///   <item><description><c>xml-model</c> - Associates a model/schema with the document</description></item>
        ///   <item><description><c>php</c> - PHP processing instructions</description></item>
        /// </list>
        /// <para>When null or empty, all processing instructions are preserved (if mode includes PIs).</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Only preserve xml-stylesheet PIs
        /// settings.PreservePITargets = new List&lt;string&gt; { "xml-stylesheet" };
        ///
        /// // Preserve all PIs
        /// settings.PreservePITargets = null;
        /// </code>
        /// </example>
        public List<string>? PreservePITargets { get; set; }

        /// <summary>
        /// Gets or sets whether processing instruction targets are treated case-sensitively.
        /// </summary>
        /// <remarks>
        /// The XML specification defines processing instruction targets as case-sensitive.
        /// However, some systems may use case-insensitive matching for compatibility.
        /// </remarks>
        /// <example>
        /// <code>
        /// settings.CaseSensitivePITargets = false; // "XML-STYLESHEET" matches "xml-stylesheet"
        /// </code>
        /// </example>
        public bool CaseSensitivePITargets { get; set; } = true;

        /// <summary>
        /// Creates a new instance with all preservation enabled.
        /// </summary>
        /// <returns>A new <see cref="XmlNodePreservationSettings"/> with all nodes preserved.</returns>
        /// <example>
        /// <code>
        /// var settings = XmlNodePreservationSettings.PreserveAll();
        /// </code>
        /// </example>
        public static XmlNodePreservationSettings PreserveAll()
        {
            return new XmlNodePreservationSettings
            {
                Mode = XmlNodePreservationMode.PreserveAll,
                TrackCommentPosition = true,
                NormalizeCDataWhitespace = true
            };
        }

        /// <summary>
        /// Creates a new instance with comments only preserved.
        /// </summary>
        /// <returns>A new <see cref="XmlNodePreservationSettings"/> with only comments preserved.</returns>
        /// <example>
        /// <code>
        /// var settings = XmlNodePreservationSettings.PreserveCommentsOnly();
        /// </code>
        /// </example>
        public static XmlNodePreservationSettings PreserveCommentsOnly()
        {
            return new XmlNodePreservationSettings
            {
                Mode = XmlNodePreservationMode.CommentsOnly,
                TrackCommentPosition = true
            };
        }

        /// <summary>
        /// Creates a new instance with CDATA only preserved.
        /// </summary>
        /// <returns>A new <see cref="XmlNodePreservationSettings"/> with only CDATA preserved.</returns>
        /// <example>
        /// <code>
        /// var settings = XmlNodePreservationSettings.PreserveCDataOnly();
        /// </code>
        /// </example>
        public static XmlNodePreservationSettings PreserveCDataOnly()
        {
            return new XmlNodePreservationSettings
            {
                Mode = XmlNodePreservationMode.CDataOnly,
                NormalizeCDataWhitespace = false
            };
        }
    }

    /// <summary>
    /// Defines which non-element XML nodes to preserve during comparison.
    /// </summary>
    /// <remarks>
    /// <para>By default, CDATA sections, comments, and processing instructions are stripped during
    /// document loading to focus on element and attribute differences. Use these flags to enable
    /// preservation of specific node types.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Preserve all non-element nodes
    /// settings.Mode = XmlNodePreservationMode.PreserveAll;
    ///
    /// // Preserve only comments
    /// settings.Mode = XmlNodePreservationMode.CommentsOnly;
    ///
    /// // Preserve only CDATA sections
    /// settings.Mode = XmlNodePreservationMode.CDataOnly;
    ///
    /// // Preserve only processing instructions
    /// settings.Mode = XmlNodePreservationMode.ProcessingInstructionsOnly;
    /// </code>
    /// </example>
    public enum XmlNodePreservationMode
    {
        /// <summary>
        /// No non-element nodes are preserved (default, backward compatible).
        /// </summary>
        /// <remarks>
        /// All CDATA sections, comments, and processing instructions are stripped during document loading.
        /// </remarks>
        None,

        /// <summary>
        /// Preserves all non-element nodes: CDATA, comments, and processing instructions.
        /// </summary>
        /// <remarks>
        /// All node types are preserved and compared for changes.
        /// </remarks>
        PreserveAll,

        /// <summary>
        /// Preserves only XML comments.
        /// </summary>
        /// <remarks>
        /// Only comment nodes are preserved. CDATA sections and processing instructions are stripped.
        /// </remarks>
        CommentsOnly,

        /// <summary>
        /// Preserves only CDATA sections.
        /// </summary>
        /// <remarks>
        /// Only CDATA sections are preserved. Comments and processing instructions are stripped.
        /// </remarks>
        CDataOnly,

        /// <summary>
        /// Preserves only processing instructions.
        /// </summary>
        /// <remarks>
        /// Only processing instructions are preserved. CDATA sections and comments are stripped.
        /// </remarks>
        ProcessingInstructionsOnly
    }
}
