using System.Linq;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests.Helpers
{
    /// <summary>
    /// Custom assertion helpers for diff results.
    /// </summary>
    public static class DiffAssertions
    {
        /// <summary>
        /// Asserts that the diff tree contains the expected number of nodes of a specific type.
        /// </summary>
        public static void AssertDiffContains(DiffMatch diff, DiffType type, int expectedCount = 1)
        {
            int count = CountDiffType(diff, type);
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// Asserts that at least one node in the diff tree has a path containing the specified fragment.
        /// </summary>
        public static void AssertDiffPathContains(DiffMatch diff, string pathFragment)
        {
            Assert.Contains(diff.Children, c => c.Path.Contains(pathFragment));
        }

        /// <summary>
        /// Asserts that no children of the specified diff type exist.
        /// </summary>
        public static void AssertNoChildrenOfType(DiffMatch diff, DiffType type)
        {
            Assert.DoesNotContain(diff.Children, c => c.Type == type);
        }

        /// <summary>
        /// Asserts that the summary matches the expected counts.
        /// </summary>
        public static void AssertSummaryMatches(
            DiffSummary summary,
            int total,
            int unchanged,
            int added,
            int deleted,
            int modified,
            int moved = 0)
        {
            Assert.Equal(total, summary.Total);
            Assert.Equal(unchanged, summary.Unchanged);
            Assert.Equal(added, summary.Added);
            Assert.Equal(deleted, summary.Deleted);
            Assert.Equal(modified, summary.Modified);
            Assert.Equal(moved, summary.Moved);
        }

        /// <summary>
        /// Counts the number of nodes of a specific type in the diff tree.
        /// </summary>
        private static int CountDiffType(DiffMatch diff, DiffType type)
        {
            int count = diff.Type == type ? 1 : 0;
            foreach (var child in diff.Children)
            {
                count += CountDiffType(child, type);
            }
            return count;
        }

        /// <summary>
        /// Asserts that a specific change exists at a given path.
        /// </summary>
        public static void AssertChangeAtPath(DiffMatch diff, string path, DiffType expectedType)
        {
            var match = FindAtPath(diff, path);
            Assert.NotNull(match);
            Assert.Equal(expectedType, match.Type);
        }

        /// <summary>
        /// Finds a diff node at the specified path.
        /// </summary>
        private static DiffMatch? FindAtPath(DiffMatch diff, string path)
        {
            if (diff.Path == path) return diff;

            foreach (var child in diff.Children)
            {
                var found = FindAtPath(child, path);
                if (found != null) return found;
            }

            return null;
        }
    }
}
