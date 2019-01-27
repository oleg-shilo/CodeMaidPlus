using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Win32;
using CMPlus;
using Xunit;
using static CMPlus.IndentAligner;
using AttributeData = Microsoft.CodeAnalysis.AttributeData;

namespace CMPlus.Tests
{
    class Program
    {
        static void Main()
        {
            TestAlignment4();
            // TestAlignment();
            // TestAlignment_();
            // TestFluent();
            // TestGaps();
        }

        static void AlignFile(string before, string after)
        {
            var formattedText = File.ReadAllText(before)
                                    .GetSyntaxRoot()
                                    .AlignIndents()
                                    .ToString();

            File.WriteAllText(after, formattedText);
        }

        static void TestAlignment6()
        {
            FormatCode(@"
{
    public IReadOnlyList<ScenarioInfo> FindScenarios(ScenarioDetail detailLevel)
    {
        var allScenarios = _scenarios.Values
            .Where(a => detailLevel >= a.DetailLevel)
            .OrderBy(a => a.DetailLevel)
            .ToArray();

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
        return allScenarios;
    }
}");
        }

        static void TestAlignment5()
        {
            FormatCode(@"
public OptimiserAction Create(SlideArea location)
{
    var message = $@""Error: {
                1.ToString().
   Length}"";

    var arr = new int[]{
                 1,2,3
    };

    var parameters = new List<Parameter>{
         _locationParameter.WithValue((ushort)(location == SlideArea.OutputDrawer ? 1 : 0))
    }

      var parameters = new List<Parameter>{
         _locationParameter.WithValue((ushort)(location == SlideArea.OutputDrawer ? 1 : 0))
    }
};");
        }

        static void TestAlignment()
        {
            AlignCode(@"
public void ScheduleScanIfNeeded()
{
    _diagnosticLogger.Log(
        LogEventLevel.Verbose,
        ""CameraScheduleController AwaitingScanResults = {0}"",
       _store.Camera.AwaitingScanResults);
}");
        }

        static void TestAlignment4()
        {
            AlignCode(@"
static void Main()
{
    var project =
        new Project(""MyProduct"",
                  new Dir(@""%ProgramFiles%\My Company\My Product"",
                      new File(""Program.cs"")),
                          new ManagedAction(CustomActions.MyAction));

    project.UI = WUI.WixUI_ProgressOnly;
    project.BuildMsi();
}");
        }

        static void TestAlignment2()
        {
            AlignCode(@"
{
    public static Plan Create(
    {
            if (plannedResources.Any())
            {
                if (incompleteActions.Any())
                    throw new ArgumentException(
                        $@""The following actions were not planned correctly: {
                                string.Join(""\n"", incompleteActions.Select(a => a.ToString()))
                            }"");
            }
    }
}");
        }

        static void TestAlignment1()
        {
            AlignCode(@"
   var reagentLaneScan = new ReagentLaneScan
   {
 LaneNumber = 1.ToString()
               .Length.ToString(x=>
                                {
                                    Index = 7,
                                    Count = 3,
                    Items = { 1, 2, 3}
                            })
                      .Length,

                                       TimeLoaded = 1,
                              Reagents = 1
                                    };");
        }

        static void TestAlignment3()
        {
            AlignCode(@"

var reagentsIdentified = new ReagentsIdentified
{
    Lanes = racks.Select(
        rack =>
        {
            var reagentLaneScan = new ReagentLaneScan
            {
                LaneNumber = rack.LaneNumber,
                TimeLoaded = rack.TimeLoaded,
                Reagents = rack.Positions
                             .Where(p => p.Container != null &&
                                        p.State == HvReagentPositionState.IdKnown)
                              .Select(reagent => new ReagentScan
                                {
                                    Position = reagent.Position,
                                 Upi = reagent.Container.UPI
                                }).ToList()
            };
            return reagentLaneScan;
        }).ToList()
}

            "
                     );
        }

        static void TestAlignment_()
        {
            printOut = false;
            var utf8WithBom = new System.Text.UTF8Encoding(true);

            var files = Directory.GetFiles("DomainControl", "*.cs", SearchOption.AllDirectories);
            var count = 0;
            foreach (var file in files)
            {
                var code = File.ReadAllText(file);

                var formattedCode = AlignCode(code);
                // var formattedCode = AlignFluent(code);

                if (code != formattedCode)
                {
                    File.WriteAllText(file, formattedCode, utf8WithBom);
                    Console.WriteLine($"{++count} of {files.Count()}...");
                }
            }
        }

        static bool printOut = true;

        static string AlignCode(string code)
        {
            var changes = new DecoratedView(code);
            var root = code.GetSyntaxRoot();

            root = root.AlignIndents(changes.OnLineChanged);

            if (printOut)
                Console.WriteLine(changes);

            var formattedText = root.ToFullString();
            return formattedText;
        }

        static string FormatCode(string code)
        {
            var root = code.GetSyntaxRoot()
                           .FixBrackets();

            var formattedText = root.ToString();
            Console.WriteLine(formattedText);
            return formattedText;
        }

        static string AlignFluent(string code)
        {
            var root = code.GetSyntaxRoot();

            var singleIndent = "    ";
            var lineInfo = new Dictionary<SyntaxToken, SyntaxTrivia>();

            var multilineStatements = root.DescendantTokens()

                                          .Where(x => x.IsKind(SyntaxKind.DotToken) &&
                                                       x.HasLeadingTrivia &&
                                                       x.LeadingTrivia.Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)))

                                          .Select(x => new
                                          {
                                              Token = x,
                                              Text = x.ToString(),
                                              WhitespaceTrivia = x.ParentStatementWhitespaceTrivia(),
                                          })
                                          .GroupBy(x => x.WhitespaceTrivia)
                                          .ToArray();

            foreach (var statement in multilineStatements)
            {
                var statementIndent = statement.Key.ToString();
                var firstDescendandLineIndent = statement.First().Token.LastWhitespaceTrivia().ToString();
                var desiredIndent = (statementIndent + singleIndent);

                var offset = desiredIndent.Length - firstDescendandLineIndent.Length;

                if (offset > 0)
                {
                    var extraIndent = new string(' ', offset);

                    foreach (var item in statement)
                    {
                        var itemIndent = item.Token.LastWhitespaceTrivia() + extraIndent;
                        lineInfo.Add(item.Token, SyntaxFactory.Whitespace(itemIndent));
                    }
                }
            }

            // must be done in a single hit in order to avoid invalidating tokens during the run
            root = root.ReplaceTokens(lineInfo.Keys,
                (oldToken, newToken) =>
                {
                    var newWhitespace = lineInfo[oldToken];
                    var oldWhitespaces = oldToken.LeadingTrivia
                                                 .Where(x => x.IsKind(SyntaxKind.WhitespaceTrivia));

                    var newLeadingTrivia = oldToken.ReplaceTrivia(oldWhitespaces, (x, y) => newWhitespace)
                                                   .LeadingTrivia;

                    return oldToken
                        .WithoutTrivia()
                        .WithLeadingTrivia(newLeadingTrivia)
                        .WithTrailingTrivia(oldToken.TrailingTrivia);
                });

            var formattedText = root.ToFullString();
            return formattedText;
        }

        static void TestGaps()
        {
            var root = File.ReadAllText(@"..\..\Common\TestClassA.cs")
                           .GetSyntaxRoot();

            var commentsEnds = root.DescendantNodesAndTokens(null, true)
                .Where(x => x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                .Select(node => node.GetLocation().GetLineSpan().EndLinePosition.Line)
                .ToArray();

            foreach (var line in commentsEnds)
            {
                if (root.GetLineText(line).HasNoText())
                {
                    var span = root.GetText().Lines[line].Span;

                    var trivia = root.DescendantTrivia(null, true)
                                     .FirstOrDefault(x => x.Span.IntersectsWith(span) &&
                                                          x.ToString().HasNoText());

                    var token = trivia.Token;

                    root = root.ReplaceToken(token, token.RemoveFromLeadingTrivia(trivia));
                    var text = root.ToFullString();
                }
            }

            Console.WriteLine($"====================");
            for (int i = 0; i < root.GetText().Lines.Count; i++)
                Console.WriteLine($"{i}: {root.GetText().Lines[i]}");
        }

        static void TestFluent()
        {
            var code = File.ReadAllText(@"..\..\Common\TestClassA.cs");
            Console.WriteLine(AlignFluent(code));
        }
    }

    public class TestBase
    {
        static TestBase()
        {
            Runtime.Settings = new Settings { AlignIndents = true, RemoveXmlDocGaps = true, SortUsings = true };
        }
    }

    public static class Global
    {
        public static SyntaxNode GetSyntaxRoot(this string code)
            => CSharpSyntaxTree.ParseText(code).GetRoot();

        public static string[] GetLines(this string text)
            => text.Replace("\r\n", "\n").Split('\n');
    }
}