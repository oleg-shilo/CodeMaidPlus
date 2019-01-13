using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32;
using CMPlus;
using Xunit;
using AttributeData = Microsoft.CodeAnalysis.AttributeData;
using Microsoft.CodeAnalysis.Text;

namespace CMPlus.Tests
{
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

    class Program
    {
        static void Main()
        {
            TestAlignment();
            //TestAlignment_();
            // TestFluent();
            // TestGaps();
        }

        static void TestFluent()
        {
            var code = File.ReadAllText(@"..\..\Common\TestClassA.cs");
            Console.WriteLine(AlignFluent(code));
        }

        static void TestAlignment()
        {
            // Console.WriteLine(
            AlignCode(@"
[assembly: InternalsVisibleTo(""InstrumentControl.Proxy.Tests""),
           InternalsVisibleTo(""DynamicProxyGenAssembly2""),
           InternalsVisibleTo(""TATSimulator"")]
    var map = new Dictionary<int, int>
    {
        {1, 2},
         {3, 4}
    }

var reagentsIdentified = new ReagentsIdentified
{
    Console.WriteLine(
                       1,
                       2
                );

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

            "//)
                     ); ;
            //             Console.WriteLine(AlignCode(@"
            //     enum Test
            //     {
            //         one,
            //         two
            //     }

            // ")); return;
            // Console.WriteLine(AlignCode(File.ReadAllText(@"..\..\Common\TestClassB.cs"))); return;
        }

        static void TestAlignment_()
        {
            var utf8WithBom = new System.Text.UTF8Encoding(true);

            var files = Directory.GetFiles(@"D:\dev\TMS\taipan-app-master\DomainControl", "*.cs", SearchOption.AllDirectories);
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

        static string AlignCode(string code)
        {
            var root = code.GetSyntaxRoot();

            root = root.AlignIndents(Console.WriteLine);

            var formattedText = root.ToFullString();
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
    }
}