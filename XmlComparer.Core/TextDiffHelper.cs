using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides text tokenization and word-level diffing utilities.
    /// </summary>
    /// <remarks>
    /// <para>This class provides utilities for breaking text into tokens and computing
    /// word-level differences between two strings. It uses <see cref="LcsHelper"/> internally
    /// to find the longest common subsequence of tokens.</para>
    /// <para>Tokenization splits text at whitespace and common punctuation marks,
    /// preserving punctuation as separate tokens. This enables fine-grained text diffing
    /// that shows which words changed rather than treating entire text blocks as changed.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string oldText = "The quick brown fox";
    /// string newText = "The fast brown cat";
    ///
    /// var diffs = TextDiffHelper.GetDiffs(oldText, newText);
    /// // Result: [
    /// //   ("The", Unchanged),
    /// //   ("quick", Deleted),
    /// //   ("fast", Added),
    /// //   ("brown", Unchanged),
    /// //   ("fox", Deleted),
    /// //   ("cat", Added)
    /// // ]
    /// </code>
    /// </example>
    public static class TextDiffHelper
    {
        /// <summary>
        /// Tokenizes text into words and punctuation marks.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>A list of tokens extracted from the text.</returns>
        /// <remarks>
        /// <para>The tokenization pattern splits on whitespace and common punctuation marks
        /// (period, comma, semicolon, exclamation mark, colon, question mark), treating them
        /// as separate tokens. This preserves punctuation for accurate diffing.</para>
        /// <para>Empty tokens are filtered out from the result.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var tokens = TextDiffHelper.Tokenize("Hello, world!");
        /// // Result: ["Hello", ",", " ", "world", "!"]
        /// </code>
        /// </example>
        public static List<string> Tokenize(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();
            return Regex.Split(text, @"(\s+|[.,;!?:])").Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        /// <summary>
        /// Computes word-level differences between two text strings.
        /// </summary>
        /// <param name="oldText">The original text.</param>
        /// <param name="newText">The modified text.</param>
        /// <returns>A list of token-diff type pairs representing the word-level diff.</returns>
        /// <remarks>
        /// <para>This method tokenizes both texts, finds the longest common subsequence using
        /// <see cref="LcsHelper.FindLcs{T}"/>, and then walks through both sequences to
        /// identify which tokens were added, deleted, or remain unchanged.</para>
        /// <para>The returned list represents a linear sequence of tokens interleaved with
        /// their diff types. This can be used for generating word-level HTML diffs or
        /// text-based difference reports.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var diffs = TextDiffHelper.GetDiffs(
        ///     "The quick brown fox jumps.",
        ///     "The fast brown cat jumps!"
        /// );
        ///
        /// foreach (var (token, type) in diffs)
        /// {
        ///     Console.Write($"[{type}] {token} ");
        /// }
        /// // Output: [Unchanged] The [Deleted] quick [Added] fast [Unchanged] brown ...
        /// </code>
        /// </example>
        public static List<(string Token, DiffType Type)> GetDiffs(string oldText, string newText)
        {
            var oldTokens = Tokenize(oldText);
            var newTokens = Tokenize(newText);

            var lcs = LcsHelper.FindLcs(oldTokens, newTokens);

            var result = new List<(string Token, DiffType Type)>();

            int i = 0; // old index
            int j = 0; // new index

            foreach (var token in lcs)
            {
                // Consume deleted tokens (in old but not in LCS)
                while (i < oldTokens.Count && oldTokens[i] != token)
                {
                    result.Add((oldTokens[i], DiffType.Deleted));
                    i++;
                }
                // Consume added tokens (in new but not in LCS)
                while (j < newTokens.Count && newTokens[j] != token)
                {
                    result.Add((newTokens[j], DiffType.Added));
                    j++;
                }

                // Consume matching token (in both LCS)
                if (i < oldTokens.Count && j < newTokens.Count)
                {
                    result.Add((oldTokens[i], DiffType.Unchanged));
                    i++;
                    j++;
                }
            }

            // Flush remaining tokens
            while (i < oldTokens.Count) { result.Add((oldTokens[i], DiffType.Deleted)); i++; }
            while (j < newTokens.Count) { result.Add((newTokens[j], DiffType.Added)); j++; }

            return result;
        }
    }
}
