using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using CMPlus;
using System.Collections.Generic;

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
            TestFluent();
            // TestGaps();
        }

        static void TestFluent()
        {
            var root = File.ReadAllText(@"..\..\Common\TestClassA.cs")
                           .GetSyntaxRoot();

            var lineInfo = new Dictionary<SyntaxToken, SyntaxTrivia>();

            // var test = root.DescendantTokens()

            //                                .Where(x => x.IsKind(SyntaxKind.DotToken) &&
            //                                            x.HasLeadingTrivia &&
            //                                            x.LeadingTrivia.Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)))

            //                                .Select(x => new
            //                                {
            //                                    Token = x,
            //                                    Line = x.EndLineNumber(),
            //                                    ParentStatement = x.ParentStatement()
            //                                    // .GetLeadingTrivia().LastOrDefault(y => y.IsKind(SyntaxKind.WhitespaceTrivia))
            //                                })
            //                                .GroupBy(x => x.ParentStatement)
            //                                .ToArray();

            var linesStartingWithDot = root.DescendantTokens()

                                           .Where(x => x.IsKind(SyntaxKind.DotToken) &&
                                                       x.HasLeadingTrivia &&
                                                       x.LeadingTrivia.Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)))

                                           .Select(x => new
                                           {
                                               Token = x,
                                               WhitespaceTrivia = x.ParentStatementWhitespaceTrivia(),
                                           })
                                           .GroupBy(x => x.WhitespaceTrivia)
                                           .ToArray();

            // var result = priceLog.GroupBy(s => s.LogDateTime.ToString("MMM yyyy")).Select(grp => new PriceLog() { LogDateTime = Convert.ToDateTime(grp.Key), Price = (int)grp.Average(p => p.Price) }).ToList()

            /// must be done in a single hit
            foreach (var block in linesStartingWithDot)
            {
                var minIndent = block.Key.ToString();
                var whiteSpace = SyntaxFactory.Whitespace(minIndent);
                foreach (var item in block)
                    lineInfo.Add(item.Token, whiteSpace);
            }

            root = root.ReplaceTokens(lineInfo.Keys,
                (oldToken, newToken) =>
                {
                    // var indent = lineInfo[oldToken].indent;
                    var oldWhitespaces = oldToken.LeadingTrivia.Where(x => x.IsKind(SyntaxKind.WhitespaceTrivia));

                    return oldToken.WithoutTrivia()
                               .WithLeadingTrivia(oldToken.ReplaceTrivia(oldWhitespaces,
                                                                         (x, y) =>
                                                                         {
                                                                             var parentIndent = oldToken.ParentStatementWhitespaceTrivia().ToString();
                                                                             return SyntaxFactory.Whitespace(parentIndent + "    ");
                                                                             // return whiteSpace;
                                                                         }).LeadingTrivia)
                               // (x, y) => lineInfo[oldToken]).LeadingTrivia)
                               .WithTrailingTrivia(oldToken.TrailingTrivia);
                });

            var text1 = root.ToFullString();
            return;

            // foreach (var dot in linesStartingWithDot.Reverse())
            // {
            //     SyntaxTrivia trivia = dot.LeadingTrivia.FirstOrDefault(x => x.IsKind(SyntaxKind.WhitespaceTrivia));
            //     var token = trivia.Token;
            //     var line = dot.GetLocation().GetLineSpan().EndLinePosition.Line;
            //     Debug.WriteLine($"{line}: {trivia}{token}");

            //     if (line == 19)
            //         root = root.ReplaceToken(token, token.WithoutTrivia()
            //                                          // .WithLeadingTrivia(whiteSpace)
            //                                          .WithLeadingTrivia(SyntaxTriviaList.Empty)
            //                                          .WithTrailingTrivia(token.TrailingTrivia));
            //     // root = root.ReplaceTrivia(trivia, whiteSpace);
            //     //root = root.ReplaceToken(token, token.RemoveFromLeadingTrivia(trivia));
            // }

            // var text = root.ToFullString();
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