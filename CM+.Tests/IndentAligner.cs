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

    public class IndentAligner
    {
        [Fact(Skip = "The functionality is not implemented yet")]
        public void Align_BracesBock()
        {
            var code =
@" var map = new Dictionary<int, int>
 {
    { 1, 2 },
                 { 3, 4 }
 }".GetSyntaxRoot();
            var processedCode = code.AlignIndents()
                                    .ToString()
                                    .GetLines();

            Assert.Equal(" {", processedCode[1]);
            Assert.Equal("     { 1, 2 },", processedCode[2]);
            Assert.Equal("     { 3, 4 }", processedCode[3]);
            Assert.Equal(" }", processedCode[4]);
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

            var processedCode = code.AlignIndents(x => Debug.WriteLine(x))
                                    .ToString()
                                    .GetLines();

            Assert.Equal("{", processedCode[1]);
            Assert.Equal("    { 1, 2 },", processedCode[2]);
            Assert.Equal("    { 3, 4 }", processedCode[3]);
            Assert.Equal("}", processedCode[4]);
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
    }
}