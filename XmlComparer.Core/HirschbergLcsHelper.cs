using System;
using System.Collections.Generic;

namespace XmlComparer.Core
{
    /// <summary>
    /// Provides space-optimized LCS computation using Hirschberg's algorithm.
    /// </summary>
    /// <remarks>
    /// <para>Hirschberg's algorithm finds the longest common subsequence in O(m*n) time
    /// but only O(min(m,n)) space, making it suitable for large documents.</para>
    /// <para>The algorithm uses a divide-and-conquer approach:</para>
    /// <list type="number">
    ///   <item><description>Split one sequence in half</description></item>
    ///   <item><description>Find the optimal split point in the other sequence</description></item>
    ///   <item><description>Recursively solve the two subproblems</description></item>
    ///   <item><description>Combine the results</description></item>
    /// </list>
    /// <para>Space complexity: O(min(m,n)) where m and n are the sequence lengths.</para>
    /// <para>Time complexity: O(m*n).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var a = new[] { 1, 2, 3, 4, 5 };
    /// var b = new[] { 2, 4, 6, 8 };
    ///
    /// var lcs = HirschbergLcsHelper.Compute(a, b);
    /// // lcs = { 2, 4 }
    /// </code>
    /// </example>
    public static class HirschbergLcsHelper
    {
        /// <summary>
        /// Computes the LCS of two sequences using Hirschberg's algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequences.</typeparam>
        /// <param name="sequenceA">The first sequence.</param>
        /// <param name="sequenceB">The second sequence.</param>
        /// <param name="comparer">The equality comparer for elements.</param>
        /// <returns>The LCS as a list of pairs (indexA, indexB).</returns>
        /// <remarks>
        /// The result is a list of index pairs indicating matching positions.
        /// </remarks>
        public static List<(int IndexA, int IndexB)> Compute<T>(
            IReadOnlyList<T> sequenceA,
            IReadOnlyList<T> sequenceB,
            IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            var result = new List<(int, int)>();
            HirschbergRecursive(sequenceA, sequenceB, 0, sequenceA.Count, 0, sequenceB.Count, result, comparer);
            return result;
        }

        /// <summary>
        /// Computes just the length of the LCS (more efficient than getting the full LCS).
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequences.</typeparam>
        /// <param name="sequenceA">The first sequence.</param>
        /// <param name="sequenceB">The second sequence.</param>
        /// <param name="comparer">The equality comparer for elements.</param>
        /// <returns>The length of the LCS.</returns>
        public static int ComputeLength<T>(
            IReadOnlyList<T> sequenceA,
            IReadOnlyList<T> sequenceB,
            IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            return ComputeLastRow(sequenceA, sequenceB, comparer)[sequenceB.Count];
        }

        /// <summary>
        /// Recursive Hirschberg algorithm implementation.
        /// </summary>
        private static void HirschbergRecursive<T>(
            IReadOnlyList<T> a,
            IReadOnlyList<T> b,
            int aStart, int aEnd,
            int bStart, int bEnd,
            List<(int, int)> result,
            IEqualityComparer<T> comparer)
        {
            // Base cases
            if (aEnd == aStart)
            {
                // No elements in A, nothing to match
                return;
            }

            if (bEnd == bStart)
            {
                // No elements in B, nothing to match
                return;
            }

            if (aEnd - aStart == 1)
            {
                // Single element in A, find its match in B
                for (int j = bStart; j < bEnd; j++)
                {
                    if (comparer.Equals(a[aStart], b[j]))
                    {
                        result.Add((aStart, j));
                        return;
                    }
                }
                return;
            }

            // Divide A in half
            int aMid = aStart + (aEnd - aStart) / 2;

            // Compute LCS lengths from start to mid
            var forwardLcs = ComputeLastRow(
                Subsequence(a, aStart, aMid),
                Subsequence(b, bStart, bEnd),
                comparer);

            // Compute LCS lengths from end to mid (reverse)
            var reverseLcs = ComputeLastRow(
                ReverseSubsequence(a, aMid, aEnd),
                ReverseSubsequence(b, bStart, bEnd),
                comparer);

            // Find the optimal split point in B
            int maxLen = -1;
            int bSplit = bStart;

            int bLen = bEnd - bStart;
            for (int j = 0; j <= bLen; j++)
            {
                int len = forwardLcs[j] + reverseLcs[bLen - j];
                if (len > maxLen)
                {
                    maxLen = len;
                    bSplit = bStart + j;
                }
            }

            // Recursively solve the two subproblems
            HirschbergRecursive(a, b, aStart, aMid, bStart, bSplit, result, comparer);
            HirschbergRecursive(a, b, aMid, aEnd, bSplit, bEnd, result, comparer);
        }

        /// <summary>
        /// Computes the last row of the LCS DP table using only O(n) space.
        /// </summary>
        private static int[] ComputeLastRow<T>(
            IReadOnlyList<T> a,
            IReadOnlyList<T> b,
            IEqualityComparer<T> comparer)
        {
            int m = a.Count;
            int n = b.Count;

            // Use two rows instead of full table
            var prev = new int[n + 1];
            var curr = new int[n + 1];

            for (int i = 1; i <= m; i++)
            {
                curr[0] = 0;

                for (int j = 1; j <= n; j++)
                {
                    if (comparer.Equals(a[i - 1], b[j - 1]))
                    {
                        curr[j] = prev[j - 1] + 1;
                    }
                    else
                    {
                        curr[j] = Math.Max(prev[j], curr[j - 1]);
                    }
                }

                // Swap rows
                var temp = prev;
                prev = curr;
                curr = temp;
            }

            return prev;
        }

        /// <summary>
        /// Extracts a subsequence from a sequence.
        /// </summary>
        private static List<T> Subsequence<T>(IReadOnlyList<T> source, int start, int end)
        {
            var result = new List<T>(end - start);
            for (int i = start; i < end; i++)
            {
                result.Add(source[i]);
            }
            return result;
        }

        /// <summary>
        /// Extracts a reversed subsequence from a sequence.
        /// </summary>
        private static List<T> ReverseSubsequence<T>(IReadOnlyList<T> source, int start, int end)
        {
            var result = new List<T>(end - start);
            for (int i = end - 1; i >= start; i--)
            {
                result.Add(source[i]);
            }
            return result;
        }

        /// <summary>
        /// Computes the LCS using the standard O(m*n) space algorithm.
        /// </summary>
        /// <remarks>
        /// This method is provided for comparison and fallback.
        /// Use <see cref="Compute"/> for the space-efficient version.
        /// </remarks>
        public static List<(int IndexA, int IndexB)> ComputeStandard<T>(
            IReadOnlyList<T> sequenceA,
            IReadOnlyList<T> sequenceB,
            IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            int m = sequenceA.Count;
            int n = sequenceB.Count;

            // Create DP table
            var dp = new int[m + 1, n + 1];

            // Fill table
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (comparer.Equals(sequenceA[i - 1], sequenceB[j - 1]))
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            // Backtrack to find LCS
            var result = new List<(int, int)>();
            int iA = m, iB = n;

            while (iA > 0 && iB > 0)
            {
                if (comparer.Equals(sequenceA[iA - 1], sequenceB[iB - 1]))
                {
                    result.Add((iA - 1, iB - 1));
                    iA--;
                    iB--;
                }
                else if (dp[iA - 1, iB] > dp[iA, iB - 1])
                {
                    iA--;
                }
                else
                {
                    iB--;
                }
            }

            result.Reverse();
            return result;
        }
    }
}
