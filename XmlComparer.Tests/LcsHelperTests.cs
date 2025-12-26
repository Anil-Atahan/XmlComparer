using System.Collections.Generic;
using Xunit;
using XmlComparer.Core;

namespace XmlComparer.Tests
{
    public class LcsHelperTests
    {
        [Fact]
        public void FindLcs_ShouldReturnLongestCommonSubsequence_Integers()
        {
            var seq1 = new List<int> { 1, 2, 3, 4, 5 };
            var seq2 = new List<int> { 1, 3, 5 };

            var lcs = LcsHelper.FindLcs(seq1, seq2);

            Assert.Equal(3, lcs.Count);
            Assert.Equal(new List<int> { 1, 3, 5 }, lcs);
        }

        [Fact]
        public void FindLcs_ShouldReturnEmpty_WhenNoCommonElements()
        {
            var seq1 = new List<int> { 1, 2, 3 };
            var seq2 = new List<int> { 4, 5, 6 };

            var lcs = LcsHelper.FindLcs(seq1, seq2);

            Assert.Empty(lcs);
        }

        [Fact]
        public void FindLcs_ShouldHandleStrings()
        {
            var seq1 = new List<string> { "A", "B", "C", "D" };
            var seq2 = new List<string> { "B", "D" };

            var lcs = LcsHelper.FindLcs(seq1, seq2);

            Assert.Equal(new List<string> { "B", "D" }, lcs);
        }
    }
}
