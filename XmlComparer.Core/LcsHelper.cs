using System;
using System.Collections.Generic;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides Longest Common Subsequence (LCS) computation for comparing sequences.
    /// </summary>
    /// <remarks>
    /// <para>This static class implements the classic dynamic programming algorithm for finding
    /// the longest common subsequence between two sequences. LCS is used in diff algorithms
    /// to identify the longest sequence of elements that appear in both inputs in the same order.</para>
    /// <para>The time complexity is O(m*n) where m and n are the lengths of the input sequences.
    /// The space complexity is also O(m*n) for the DP table.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var seq1 = new List&lt;string&gt; { "A", "B", "C", "D" };
    /// var seq2 = new List&lt;string&gt; { "A", "X", "C", "Y", "D" };
    ///
    /// var lcs = LcsHelper.FindLcs(seq1, seq2);  // Returns: ["A", "C", "D"]
    /// </code>
    /// </example>
    public static class LcsHelper
    {
        /// <summary>
        /// Finds the longest common subsequence between two sequences.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequences.</typeparam>
        /// <param name="seq1">The first sequence.</param>
        /// <param name="seq2">The second sequence.</param>
        /// <param name="comparer">Optional custom equality comparer. If null, uses default object equality.</param>
        /// <returns>A list containing the elements of the longest common subsequence.</returns>
        /// <remarks>
        /// <para>The returned subsequence maintains the relative order of elements as they appear
        /// in both input sequences. Elements that appear in both sequences but in different orders
        /// will not be included in the LCS.</para>
        /// <para>If there are multiple common subsequences of the same maximum length,
        /// one of them will be returned (which one is implementation-dependent).</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Default equality comparison
        /// var words1 = new List&lt;string&gt; { "the", "quick", "brown", "fox" };
        /// var words2 = new List&lt;string&gt; { "the", "brown", "dog" };
        /// var common = LcsHelper.FindLcs(words1, words2);
        /// // Result: ["the", "brown"]
        ///
        /// // Custom comparison
        /// var nums1 = new List&lt;int&gt; { 1, 2, 3, 4 };
        /// var nums2 = new List&lt;int&gt; { 1, 5, 3, 6 };
        /// var commonNums = LcsHelper.FindLcs(nums1, nums2);
        /// // Result: [1, 3]
        /// </code>
        /// </example>
        public static List<T> FindLcs<T>(List<T> seq1, List<T> seq2, Func<T, T, bool>? comparer = null)
        {
            comparer ??= (a, b) => object.Equals(a, b);

            int m = seq1.Count;
            int n = seq2.Count;
            int[,] C = new int[m + 1, n + 1];

            // Build the DP table
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (comparer(seq1[i - 1], seq2[j - 1]))
                    {
                        C[i, j] = C[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        C[i, j] = Math.Max(C[i, j - 1], C[i - 1, j]);
                    }
                }
            }

            // Backtrack to find the LCS
            var result = new List<T>();
            int x = m, y = n;
            while (x > 0 && y > 0)
            {
                if (comparer(seq1[x - 1], seq2[y - 1]))
                {
                    result.Add(seq1[x - 1]);
                    x--;
                    y--;
                }
                else if (C[x - 1, y] > C[x, y - 1])
                    x--;
                else
                    y--;
            }

            result.Reverse();
            return result;
        }
    }
}
