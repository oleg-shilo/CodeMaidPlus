using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CMPlus
{
    public static class IndentAligner
    {
        public static SyntaxNode AlignIndents(this SyntaxNode root)
        {
            var singleIndent = "    ";
            var lineInfo = new Dictionary<SyntaxToken, SyntaxTrivia>();

            // collect all lines that start with '.' and belong to the same statement
            var multilineSTatements = root.DescendantTokens()
                                          .Where(x => x.IsKind(SyntaxKind.DotToken) &&
                                                      x.HasLeadingTrivia &&
                                                      x.LeadingTrivia.Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)))
                                          .Select(x => new
                                          {
                                              Token = x,
                                              WhitespaceTrivia = x.ParentStatementWhitespaceTrivia(),
                                          })
                                          .GroupBy(x => x.WhitespaceTrivia);

            // determine if the lines are not indented at least by a signle level comparing to the statement
            // root indent.
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

            // Shift all previously identified lines.
            // Must be done in a single hit in order to avoid invalidating tokens during the run.
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
            return root;
        }
    }
}