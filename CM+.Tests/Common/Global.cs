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
            // TestFluent();
            // TestGaps();
        }

        static string singleIndent = "    ";
        static string[] separators = { "    " };
        static IEnumerable<int> globalIndentPoints = new List<int>();

        static IEnumerable<int> FindIndentPoints(string text)
        {
            var indentPoints = new List<int>();

            for (int i = 0; i < text.Length - 1; i++)
            {
                // capture all de-referencings
                if (text[i] == '.')
                    indentPoints.Add(i);
                // capture first position of the argument (in the future can be a part of "arg list alignment")
                else if (text[i] == '(')
                    indentPoints.Add(i + 1);
                // capture first indent of the block
                else if (text[i] == '{')
                {
                    indentPoints.Add(i + singleIndent.Length);
                    indentPoints.Add(i);
                }
            }
            return indentPoints;
        }

        static int FindBestIndentAlignment(int currentIndent, string line, string prevLine)
        {
            bool alwaysAlignToNext = false;

            // if (currentIndent % singleIndent.Length == 0)  // TBD
            //     return currentIndent;

            var indentPoints = new List<int>();

            prevLine = prevLine ?? new string(' ', currentIndent);

            var prevIndentLength = prevLine.GetIndentLength();
            indentPoints.Add(prevIndentLength);
            indentPoints.Add(prevIndentLength + singleIndent.Length);

            // Being fluent criteria - "starts with dot"
            var isFluent = line.TrimStart().StartsWith(".");
            var isPrevFluent = prevLine.TrimStart().StartsWith(".");

            // add left side indent points for if the current line is a start or an end of the code block

            if (prevLine.TrimEnd().EndsWith(";") || // prev statement is completed
                prevLine.TrimEnd().EndsWith("}") || // prev statement is completed
                prevLine.TrimEnd().EndsWith(")") || // prev statement is completed
                line.TrimStart().StartsWith("{") || // current statement is a block start
                line.TrimStart().StartsWith("}") || // current statement is a block end
                prevLine.TrimEnd().EndsWith("{")
                )
            {
                for (int i = 0; i < prevLine.GetIndentLength() / singleIndent.Length + 1; i++)
                // for (int i = 0; i < prevLine?.Length / singleIndent.Length + 1; i++) // TBD
                {
                    indentPoints.Add(singleIndent.Length * (i + 1));
                }
            }

            indentPoints.AddRange(globalIndentPoints);

            // else
            //     for (int i = 0; i < prevLine?.Length / singleIndent.Length + 1; i++) // TBD
            //     {
            //         indentPoints.Add(singleIndent.Length * (i + 1));
            //     }

            indentPoints.RemoveAll(x => x > prevLine.GetIndentLength() + singleIndent.Length);

            for (int i = 0; i < prevLine.Length - 1; i++)
            {
                // capture all de-referencings
                if (prevLine[i] == '.')
                    indentPoints.Add(i);
                // capture first position of the argument (in the future can be a part of "arg list alignment")
                else if (prevLine[i] == '(')
                    indentPoints.Add(i + 1);
                // capture first indent of the block
                else if (prevLine[i] == '{')
                    indentPoints.Add(i + singleIndent.Length);
                // do not capture "x => x" start of the second 'x'
                else if (!isFluent && prevLine[i] == ' ' && prevLine[i + 1] != ' ' && prevLine[i + 1] != '=')
                    indentPoints.Add(i + 1);
            }

            if (isFluent && !isPrevFluent)
            {
                indentPoints.Remove(prevIndentLength);
            }

            if (indentPoints.Contains(currentIndent))
                return currentIndent;

            indentPoints = indentPoints.Distinct().OrderBy(x => x).ToList();

            if (indentPoints.Last() <= currentIndent)
                return indentPoints.Last();

            if (currentIndent <= indentPoints.First())
                return indentPoints.First();

            var pointsBefore = indentPoints.TakeWhile(x => x < currentIndent);

            var pointBefore = pointsBefore.Last();
            var pointAfter = indentPoints[pointsBefore.Count()];

            // if (alwaysAlignToNext || ((currentIndent - pointBefore) >= (pointAfter - currentIndent))) // TBD
            if (alwaysAlignToNext || ((currentIndent - pointBefore) >= singleIndent.Length / 2))
            {
                return pointAfter;
            }
            else
                return pointBefore;
        }

        static void TestFluent()
        {
            var code = File.ReadAllText(@"..\..\Common\TestClassA.cs");
            Console.WriteLine(AlignFluent(code));
        }

        static void TestAlignment()
        {
            Console.WriteLine(AlignCode(@"
            var reagentsIdentified = new ReagentsIdentified
            {
                       Console.WriteLine(
                           ds
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

            ")); return;
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

            string prevNonEmptyLine = null;
            var lineInfo = new Dictionary<SyntaxToken, SyntaxTrivia>();

            var linesWithCode = root.DescendantTokens()
                                    .Where(x => x.HasLeadingTrivia &&
                                                x.LeadingTrivia.Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)));

            var lines = root.GetText().Lines;

            foreach (var startToken in linesWithCode)
            {
                bool MatchingOpenTokenIndentFor(SyntaxKind closing, out int indent)
                {
                    var success = false;
                    indent = 0;
                    if (startToken.IsKind(closing))
                    {
                        success = true;

                        // openKind = closingKind-1
                        var opening = startToken.Parent
                                                .ChildTokens()
                                                .FirstOrDefault(x => x.IsKind(closing - 1));

                        if (lineInfo.ContainsKey(opening))
                            indent = lineInfo[opening].ToString().Length;
                        else
                            indent = opening.GetLocation().GetLineSpan().StartLinePosition.Character;
                    }
                    return success;
                }

                var lineNumber = startToken.GetLocation().GetLineSpan().EndLinePosition.Line;

                var text = lines[lineNumber].ToString();

                if (text.HasText())
                {
                    int currentIndent = text.GetIndentLength();

                    if (currentIndent == 0)
                        continue; // start of the line, no need to align

                    int bestIndent = currentIndent;

                    // startToken is a closing token, which has matching opening token
                    // on a different line. This is because the closing token is the first token in the line.
                    if (MatchingOpenTokenIndentFor(SyntaxKind.CloseBraceToken, out int indent))
                        bestIndent = indent;
                    else if (MatchingOpenTokenIndentFor(SyntaxKind.CloseParenToken, out indent))
                        bestIndent = indent;
                    else
                        bestIndent = FindBestIndentAlignment(currentIndent, text, prevNonEmptyLine);

                    if (currentIndent != bestIndent)
                    {
                        var newItemIndent = new string(' ', bestIndent);
                        prevNonEmptyLine = newItemIndent + text.TrimStart();

                        globalIndentPoints = globalIndentPoints
                                                .Where(x => x < prevNonEmptyLine.GetIndentLength())
                                                .Concat(FindIndentPoints(prevNonEmptyLine))
                                                .OrderBy(x => x)
                                                .ToArray();

                        lineInfo.Add(startToken, SyntaxFactory.Whitespace(newItemIndent));

                        continue;
                    }
                }

                prevNonEmptyLine = text;
                globalIndentPoints = globalIndentPoints
                                                .Where(x => x < prevNonEmptyLine.GetIndentLength())
                                                .Concat(FindIndentPoints(prevNonEmptyLine))
                                                .OrderBy(x => x)
                                                .ToArray();
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

            // root = IndentAligner.AlignIndents(root);
            var formattedText = root.ToFullString();
            // Console.WriteLine(formattedText);
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