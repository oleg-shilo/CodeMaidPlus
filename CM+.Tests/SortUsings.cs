using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CMPlus.Tests
{
    public class SortUsings : TestBase
    {
        [Fact]
        public void Sort_Alphabetically()
        {
            var code =
@"using Xunit;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System;

class Test
{
}";
            var processedCode = code.GetSyntaxRoot()
                                    .SortUsings()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("using System;", processedCode[0]);
            Assert.Equal("using System.Linq;", processedCode[1]);
            Assert.Equal("using Microsoft.CodeAnalysis.CSharp;", processedCode[2]);
            Assert.Equal("using Xunit;", processedCode[3]);
        }

        [Fact]
        public void Sort_AliasesAndStatics()
        {
            var code =
@"using Xunit;
using static System.Console;
using System.Linq;
using LNQ = System.Linq;
using System;

class Test
{
}";

            var processedCode = code.GetSyntaxRoot()
                                    .SortUsings()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("using System;", processedCode[0]);
            Assert.Equal("using System.Linq;", processedCode[1]);
            Assert.Equal("using Xunit;", processedCode[2]);
            Assert.Equal("using static System.Console;", processedCode[3]);
            Assert.Equal("using LNQ = System.Linq;", processedCode[4]);
        }

        [Fact]
        public void Remove_Duplications()
        {
            var code =
@"using Xunit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Linq;
using System.Linq;
using System;

class Test
{
}";
            var processedCode = code.GetSyntaxRoot()
                                    .SortUsings()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("using System;", processedCode[0]);
            Assert.Equal("using System.Linq;", processedCode[1]);
            Assert.Equal("using Microsoft.CodeAnalysis.CSharp;", processedCode[2]);
            Assert.Equal("using Xunit;", processedCode[3]);
        }
    }
}