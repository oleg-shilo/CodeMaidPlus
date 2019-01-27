using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CMPlus.Tests
{
    public class BracketsNormalizer : TestBase
    {
        [Fact]
        public void Fix_EgyptianBrackets()
        {
            var code =
@"public OptimiserAction Create(SlideArea location)
{
    var parameters = new List<Parameter>{
         _locationParameter.WithValue((ushort)(location == SlideArea.OutputDrawer ? 1 : 0))
    }
}";
            var processedCode = code.GetSyntaxRoot()
                                    .FixBrackets()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("    var parameters = new List<Parameter>", processedCode[2]);
            Assert.Equal("    {", processedCode[3]);
            Assert.Equal("    }", processedCode[5]);
        }

        [Fact]
        public void DoesNot_Change_SameLine_Pair()
        {
            var code =
@"class Test
{
    int Index { get; }
}";
            var processedCode = code.GetSyntaxRoot()
                                    .FixBrackets()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("    int Index { get; }", processedCode[2]);
        }

        [Fact]
        public void Ignore_StringInterpolations()
        {
            var code =
@"public OptimiserAction Create(SlideArea location)
{
    var message = $@""Error: {
        1.ToString().
    Length}"";
}";
            var processedCode = code.GetSyntaxRoot()
                                    .FixBrackets()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("    var message = $@\"Error: {", processedCode[2]);
            Assert.Equal("        1.ToString().", processedCode[3]);
        }
    }
}