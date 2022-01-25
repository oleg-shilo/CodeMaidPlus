using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CMPlus
{
    public static class BracketsNormalizer
    {
        public static SyntaxNode FixBrackets(this SyntaxNode root)
        {
            if (Runtime.Settings.FixBrackets)
                try
                {
                    var indents = new Dictionary<SyntaxToken, int>();

                    // do not mess with the structured interpolations (the love child of ReSharper).
                    // They are already messed up enough:
                    //    var message = $@"Error: {1.ToString().
                    //        Length}
                    //    ";

                    var openBraces = root.DescendantTokens()
                                         .Where(x => !x.HasLeadingTrivia &&
                                                      x.IsKind(SyntaxKind.OpenBraceToken))
                                         .Where(openBrace =>
                                         {
                                             var matchingCloseBrace = openBrace.GetParentChildToken(SyntaxKind.CloseBraceToken);
                                             if (!matchingCloseBrace.IsMissing)
                                             {
                                                 return matchingCloseBrace.GetLineNumber() != openBrace.GetLineNumber();
                                             }
                                             return false;
                                         });

                    if (Runtime.Settings.DoNotAlignInterpolation)
                        openBraces = openBraces.Where(x => !x.Parent.IsKind(SyntaxKind.Interpolation));

                    var lines = root.GetText().Lines;

                    foreach (SyntaxToken braceToken in openBraces)
                    {
                        var p = braceToken.Parent;
                        var text = lines[braceToken.GetLineNumber()].ToString();

                        if (text.HasText() && text.Trim() != "{")
                            indents[braceToken] = text.GetIndentLength();
                    }

                    // must be done in a single hit in order to avoid invalidating tokens during the run
                    root = root.ReplaceTokens(indents.Keys,
                        (oldToken, newToken) =>
                        {
                            var indent = new string(' ', indents[oldToken]);

                            return oldToken
                                .WithoutTrivia()
                                .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed,
                                                   SyntaxFactory.Whitespace(indent))
                                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                        });
                    return root;
                }
                catch { }

            return root;
        }
    }
}