using System.Text.RegularExpressions;

namespace de.hmmueller.PathDxf2GCode.Tests;

[TestClass]
public class PatternMatchTests {
    private static (int from, int to)[] SimpleMatch(string input, PatternMatcher<char>.Pattern p)
        => new PatternMatcher<char>().Match(input.ToList(), p, 0).Select(ftm => (ftm.from, ftm.to)).ToArray();

    private static (int, int)[] ParseExpected(string expected)
        => expected
            .Select((c, i) => (from: i, to: i + c - '0', isStart: c != '-'))
            .Where(icv => icv.isStart)
            .Select<(int from, int to, bool), (int, int)>(icv => (icv.from, icv.to))
            .ToArray();

    [TestMethod]
    [DataRow("aabab", "--1-1")]
    public void TestSimpleLeafPattern(string input, string expected) {
        (int, int)[] expectedAsSet = ParseExpected(expected);
        (int, int)[] resultAsSet = SimpleMatch(input, new PatternMatcher<char>.LeafPattern("A", c => c == 'b'));
        CollectionAssert.AreEquivalent(expectedAsSet, resultAsSet);
    }

    [TestMethod]
    [DataRow("aabab", "-2-2-")]
    public void TestSimpleSeqPattern(string input, string expected) {
        (int, int)[] expectedAsSet = ParseExpected(expected);
        (int, int)[] resultAsSet = SimpleMatch(input,
            new PatternMatcher<char>.SeqPattern(
                new PatternMatcher<char>.LeafPattern("A", c => c == 'a'),
                new PatternMatcher<char>.LeafPattern("A", c => c == 'b'))
            );
        CollectionAssert.AreEquivalent(expectedAsSet, resultAsSet);
    }

    [TestMethod]
    [DataRow("aabab", "21-1-")]
    public void TestSimpleLoopPattern(string input, string expected) {
        (int, int)[] expectedAsSet = ParseExpected(expected);
        (int, int)[] resultAsSet = SimpleMatch(input,
            new PatternMatcher<char>.LoopPattern(
                new PatternMatcher<char>.LeafPattern("A", c => c == 'a'), 1, 4)
            );
        CollectionAssert.AreEquivalent(expectedAsSet, resultAsSet);
    }
}

