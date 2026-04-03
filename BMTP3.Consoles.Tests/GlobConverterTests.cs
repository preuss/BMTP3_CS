using System.Text.RegularExpressions;
using Xunit;
using BMTP3.Consoles.Utilities;

namespace BMTP3.Consoles.Tests
{
    public class GlobConverterTests
    {
        [Fact]
        public void StarMatchesFilename()
        {
            var re = GlobConverter.GlobToRegex("*.txt");
            Assert.True(Regex.IsMatch("file.txt", re));
            Assert.False(Regex.IsMatch("file.jpg", re));
            Assert.False(Regex.IsMatch("sub/file.txt", re));
        }

        [Fact]
        public void RecursiveMatches()
        {
            var re = GlobConverter.GlobToRegex("**/*.txt");
            Assert.True(Regex.IsMatch("a/b/c.txt", re));
            Assert.True(Regex.IsMatch("file.txt", re));
        }

        [Fact]
        public void SeparatorFlexibility()
        {
            var re = GlobConverter.GlobToRegex("sub/*.txt");
            Assert.True(Regex.IsMatch(@"sub\\file.txt", re));
            Assert.True(Regex.IsMatch("sub/file.txt", re));
        }

        [Fact]
        public void ExtglobAlternation()
        {
            var re = GlobConverter.GlobToRegex("@(foo|bar).txt");
            Assert.True(Regex.IsMatch("foo.txt", re));
            Assert.True(Regex.IsMatch("bar.txt", re));
            Assert.False(Regex.IsMatch("baz.txt", re));
        }

        [Fact]
        public void SuffixNegation()
        {
            var re = GlobConverter.GlobToRegex("*.!(jpg)");
            Assert.True(Regex.IsMatch("file.png", re));
            Assert.False(Regex.IsMatch("file.jpg", re));
        }

        [Fact]
        public void GlobalNegation()
        {
            var re = GlobConverter.GlobToRegex("!(foo)");
            Assert.True(Regex.IsMatch("bar", re));
            Assert.False(Regex.IsMatch("foo", re));
        }
    }
}
