using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CMPlus.Tests
{
    // return otherTask?.SSMNumber == jobPlanMetrics.SSMNumber
    //        && otherTask?.LastActionEndTimestamp
    //        <jobPlanMetrics.LastActionEndTimestamp;

    // return
    //         $@"TurnAroundTimeMilliseconds,{
    //                 TurnAroundTimeMilliseconds?.TotalMilliseconds
    //             },OnInstrumentTimeMilliseconds,{
    //                 OnInstrumentTimeMilliseconds?.TotalMilliseconds
    //             },TaskProcessingTimeMilliseconds,{
    //                 TaskProcessingTimeMilliseconds?.TotalMilliseconds
    //             },AverageSinceSSMCleanDuration,{
    //                 AverageSinceSSMCleanDuration?.TotalMilliseconds
    //             },AverageSinceSSMEmptyDuration,{
    //                 AverageSinceSSMEmptyDuration?.TotalMilliseconds
    //             },MaximumSinceSSMCleanDuration,{
    //                 MaximumSinceSSMCleanDuration?.TotalMilliseconds
    //             },MaximumSinceSSMEmptyDuration,{MaximumSinceSSMEmptyDuration?.TotalMilliseconds}";

    /*
     return ResourceLinks.Where(c => c.Value.Name == instance.ItemType.Name)
                         .Select(link => GetResourceType(link.Key)
                                             .NumberedInstance(link.Value.GetCorrectTarget(instance.Index)))
                         .ToList();
     *
     throw new InvalidOperationException(
                $@"Some scenarios have duplicate names: {
                        string.Join(", ",
                            allScenarios.GroupBy(a => a.Name)
                                .Where(a => a.Count() > 1)
                                .Select(a => a.Key))
                    }");

     */

    public class IndentAligner : TestBase
    {
        [Fact]
        public void Align_BracesBock()
        {
            var code = @"
    var map = new Dictionary<int, int>
    {
         { 1, 2 },
          { 3, 4 }
     }".GetSyntaxRoot();
            var processedCode = code.AlignIndents()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("    {", processedCode[1]);
            Assert.Equal("        { 1, 2 },", processedCode[2]);
            Assert.Equal("        { 3, 4 }", processedCode[3]);
            Assert.Equal("    }", processedCode[4]);
        }

        [Fact]
        public void Align_Braces()
        {
            var code =
@"var map = new Dictionary<int, int>
{
   { 1, 2 },
    { 3, 4 }
}".GetSyntaxRoot();

            var processedCode = code.AlignIndents((i, x) => Debug.WriteLine(x))
                                    .ToString()
                                    .GetLines();

            Assert.Equal("{", processedCode[1]);
            Assert.Equal("    { 1, 2 },", processedCode[2]);
            Assert.Equal("    { 3, 4 }", processedCode[3]);
            Assert.Equal("}", processedCode[4]);
        }

        [Fact]
        public void Should_Compensate_ForDots()
        {
            var code =
@"
dirItem.AddElement(
        new XElement(""Component"",
            new XAttribute(""Id?"", compId),
            new XAttribute(""Guid"", WixGuid.NewGuid(compId))));
"

.GetSyntaxRoot();

            var processedCode = code.AlignIndents((i, x) => Debug.WriteLine(x))
                                    .ToString()
                                    .GetLines();

            Assert.Equal("dirItem.AddElement(", processedCode[0]);
            Assert.Equal("        new XElement(\"Component\",", processedCode[1]);
        }

        [Fact]
        public void Should_Allow_Args_PartialAlignment()
        {
            var code =
@"
class Test
{
    void Test()
    {
        var project = new Project(""Test"",
                          new File(""file.txt""));
    }
}".GetSyntaxRoot();

            var processedCode = code.AlignIndents((i, x) => Debug.WriteLine(x))
                                    .ToString()
                                    .GetLines();

            Assert.Equal("        var project = new Project(\"Test\",", processedCode[4]);
            Assert.Equal("                          new File(\"file.txt\"));", processedCode[5]);
        }

        [Fact]
        public void Ignore_Intepolation()
        {
            var code =
@"
class Test
{
    if (allScenarios.Select(a => a.Name).Distinct().Count() != allScenarios.Length)
    {
        throw new InvalidOperationException(
            $@""Some scenarios have duplicate names: {
                    string.Join("", "",
                        allScenarios.GroupBy(a => a.Name)
                            .Where(a => a.Count() > 1)
                            .Select(a => a.Key))
                }"");
    }
}".GetSyntaxRoot();

            var processedCode = code.AlignIndents((i, x) => Debug.WriteLine(x))
                                    .ToString()
                                    .GetLines();

            // line 5 is the same as before formatting
            Assert.Equal("                    string.Join(\", \",", processedCode[6]);
        }

        [Fact]
        public void Align_Attributes()
        {
            var code = @"
[assembly: AssemblyInformationalVersion(""0.0.0.0"")]
[assembly: InternalsVisibleTo(""Infrastructure.Tests""),
         InternalsVisibleTo(""TATSimulator"")]".GetSyntaxRoot();

            var processedCode = code.AlignIndents()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("[assembly: AssemblyInformationalVersion(\"0.0.0.0\")]", processedCode[0]);
            Assert.Equal("[assembly: InternalsVisibleTo(\"Infrastructure.Tests\"),", processedCode[1]);
            Assert.Equal("           InternalsVisibleTo(\"TATSimulator\")]", processedCode[2]);
        }

        [Fact]
        public void Should_Ignore_IndentPoints_InStrings()
        {
            var code =
                @"
void Test()
{
         new Exception($@""Some scenarios .have duplicate names: {
                                  string.Join("", "",
                                      allScenarios.GroupBy(a => a.Name)
                                          .Where(a => a.Count() > 1)
                                          .Select(a => a.Key))
                    }"");
}
".GetSyntaxRoot();

            var processedCode = code.AlignIndents()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("                                  string.Join(\", \",", processedCode[3]);
        }

        [Fact]
        public void Should_Keep_ConditionalExprtesions_Aligned()
        {
            var code =
                @"
class Test
{
    void Test()
    {
        var resourceConfig = schedulerConfiguration.ResourceConfiguration
                              ?? new TaipanResourceModelConfiguration();
    }
}
".GetSyntaxRoot();

            var processedCode = code.AlignIndents()
                                    .ToString()
                                    .GetLines();

            Assert.Equal("        var resourceConfig = schedulerConfiguration.ResourceConfiguration", processedCode[4]);
            Assert.Equal("                             ?? new TaipanResourceModelConfiguration();", processedCode[5]);
        }
    }
}