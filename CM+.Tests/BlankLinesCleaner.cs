using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CMPlus.Tests
{
    public class IndentAlignment
    {
        // [assembly: AssemblyInformationalVersion("0.0.0.0")]
        // [assembly: InternalsVisibleTo("Infrastructure.Tests"),
        //    InternalsVisibleTo("TATSimulator")]

        // new Dictionary<int, int>
        // {
        //     {1, 2},
        //         {3, 4}
        // }

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
    }

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