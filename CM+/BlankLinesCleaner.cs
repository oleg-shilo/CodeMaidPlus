using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CMPlus.Tests
{
    public class BlankLinesCleaner
    {
        [Fact]
        public void Remove_SingleLine_Gaps()
        {
            var root =
@"class TestClassA
{
    /// <summary>
    /// The test1
    /// </summary>

    int test1;

    /// <summary>
    /// The test2
    /// </summary>

    int test2;
}".GetSyntaxRoot();

            var processedCode = root.RemoveXmlDocGaps()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("/// </summary>", processedCode[4].Trim());
            Assert.Equal("int test1;", processedCode[5].Trim());
            Assert.Equal("/// </summary>", processedCode[9].Trim());
            Assert.Equal("int test2;", processedCode[10].Trim());
        }

        [Fact]
        public void Remove_MultiLine_Gaps()
        {
            var root =
(@"class TestClassA
{
    /// <summary>
    /// The test1
    /// </summary>

" + @"

    int test1;
}").GetSyntaxRoot();

            var processedCode = root.RemoveXmlDocGaps()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("/// </summary>", processedCode[4].Trim());
            Assert.Equal("int test1;", processedCode[5].Trim());
        }
    }
}