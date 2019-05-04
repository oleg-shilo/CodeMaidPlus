using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CMPlus.Tests
{
    public class LinesTrimmer : TestBase
    {
        [Fact]
        public void Trimm_Line()
        {
            var (root, limit) = prepare(
@"class TestClassA
{
    int test1 = tokenA.tokenB.tokenC.t|okenD;
    int test1 = tokenA.tokenB.tokenC.tokenD;
}");

            var processedCode = root.TrimLines(limit)
                                    .ToString()
                                    .GetLines();

            Assert.Equal("    int test1 = tokenA.tokenB.tokenC", processedCode[2]);
            Assert.Equal("        .tokenD;", processedCode[3]);
            Assert.Equal("    int test1 = tokenA.tokenB.tokenC", processedCode[4]);
            Assert.Equal("        .tokenD;", processedCode[5]);
        }

        [Fact]
        public void DoNotTrim_UnbreakableLines()
        {
            var (root, limit) = prepare(
@"class TestClassA
{
    very_long_name_that_does_not_make_sense_to_|break
}");
            var processedCodeLines = root.TrimLines(limit)
                                         .ToString()
                                         .GetLines();

            Assert.Equal(4, processedCodeLines.Count());
        }

        [Fact]
        public void DoNotTrim_LastSingleCharacterToken()
        {
            var (root, limit) = prepare(
@"class TestClassA
{
    int test1 = tokenA.tokenB|(
}");

            var processedCodeLines = root.TrimLines(limit)
                                         .ToString()
                                         .GetLines();

            Assert.Equal(4, processedCodeLines.Count());
        }

        [Fact]
        public void Trim_AndBreake_StringLines()
        {
            var (root, limit) = prepare(
@"class TestClassA
{
    int test1 = tokenA.tokenB.|tokenC.tokenD;
    int test2 = ""The quick brown fox jumps over a lazy dog"" + DtateTime.Now;
}");

            var processedCodeLines = root.TrimLines(limit)
                                         .ToString()
                                         .GetLines();

            Assert.Equal(7, processedCodeLines.Count());

            Assert.Equal("    int test1 = tokenA.tokenB", processedCodeLines[2]);
            Assert.Equal("        .tokenC.tokenD;", processedCodeLines[3]);
            Assert.Equal("    int test2 = \"The quick brow\"", processedCodeLines[4]);
            Assert.Equal("        + \"n fox jumps over a lazy dog\" + DtateTime.Now;", processedCodeLines[5]);
        }

        [Fact]
        public void DoNotTrim_DeliberatelyLongLines()
        {
            var (root, limit) = prepare(
@"class TestClassA
{
    int test1 = tokenA.to|kenB.tokenC.tokenD.tokenE;
}");

            var processedCodeLines = root.TrimLines(limit)
                                         .ToString()
                                         .GetLines();

            Assert.Equal(4, processedCodeLines.Count());
        }

        (SyntaxNode, int) prepare(string code)
        {
            return (
                code.Replace("|", "").GetSyntaxRoot(),
                code.GetLines().FirstOrDefault(x => x.Contains("|")).IndexOf("|") + 1); // char position is 1-based
        }
    }
}