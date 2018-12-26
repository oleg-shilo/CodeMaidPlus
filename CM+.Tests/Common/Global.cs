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

            var singleIndent = "    ";
            var lineInfo = new Dictionary<SyntaxToken, SyntaxTrivia>();

            var multilineSTatements = root.DescendantTokens()

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

            foreach (var statement in multilineSTatements)
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

            var text1 = root.ToFullString();
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