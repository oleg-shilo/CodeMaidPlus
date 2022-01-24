using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CMPlus
{
    public static class LinesTrimmer
    {
        public static SyntaxNode TrimLines(this SyntaxNode root, int? limit = null)
        {
            if (Runtime.Settings.TrimmLines)
                try
                {
                    int lengthLimit = 36;

                    if (limit.HasValue)
                        lengthLimit = limit.Value;

                    var indents = new Dictionary<SyntaxToken, string>();
                    var nodesToReplace = new Dictionary<SyntaxNode, Func<SyntaxNode, SyntaxNode, SyntaxNode>>();

                    // `SingleLineDocumentationCommentTrivia` points to the end of the comment block
                    root.DescendantTokens()
                        .Where(x => x.HasTrailingTrivia &&
                                    x.TrailingTrivia.Any(y => y.IsKind(SyntaxKind.EndOfLineTrivia)))
                        .Select(x => new
                        {
                            Token = x,
                            Line = x.GetLineNumber()
                        })
                        .ForEach(x =>
                        {
                            var token = x.Token;

                            try
                            {
                                var lineLength = token.EndLinePositionCharacter();

                                if (lineLength >= lengthLimit)
                                {
                                    var lineTokens = token.TokensOfTheSameLine();

                                    var lengthLimitPosition = token.Span.End - (lineLength - lengthLimit);

                                    var tokensWithinLimit = lineTokens.Where(z => z.Span.End < lengthLimitPosition);

                                    // ignore a single toke lines
                                    if (tokensWithinLimit.Count() > 1)
                                    {
                                        var lastTokenWithinLimit = tokensWithinLimit.LastOrDefault();
                                        if (lastTokenWithinLimit.Text == ".")
                                        {
                                            lastTokenWithinLimit = tokensWithinLimit.Last(1);
                                        }

                                        var originalLineIndent = root.GetLineText(x.Line)
                                                                     .TakeWhile(char.IsWhiteSpace)
                                                                     .Join();

                                        var newLindeIndent = originalLineIndent + IndentAligner.Aligner.singleIndent;
                                        var tokensOutsideLimit = lineTokens.TakeAfter(tokensWithinLimit.Last());

                                        int lengthOfNewLine = originalLineIndent.Length +
                                        (tokensOutsideLimit.Last().Span.End - tokensOutsideLimit.First().Span.Start);

                                        if (tokensOutsideLimit.First().Kind() == SyntaxKind.StringLiteralToken)
                                        {
                                            lastTokenWithinLimit = tokensOutsideLimit.First();

                                            var stringExpression = lastTokenWithinLimit
                                                .ParentNode(n => n.IsKind(SyntaxKind.StringLiteralExpression) ||
                                                                 n.IsKind(SyntaxKind.InterpolatedStringExpression));

                                            nodesToReplace[stringExpression] = (oldNode, newNode) =>
                                            {
                                                var breakOffset = lengthLimitPosition - stringExpression.SpanStart;
                                                return oldNode.BrakeStringExpression(breakOffset, newLindeIndent);
                                            };
                                            return;
                                        }

                                        // do not break if remaining line length is still longer than the limit (deliberately long lines)
                                        // or if we are breaking only a single character token (e.g. '(')
                                        if (lengthOfNewLine >= lengthLimit ||
                                            (tokensOutsideLimit.Count() == 1 && tokensOutsideLimit.First().Text.Length == 1))
                                            return;

                                        indents[lastTokenWithinLimit] = originalLineIndent + IndentAligner.Aligner.singleIndent;

                                        var parentNode = lastTokenWithinLimit.Parent;

                                        nodesToReplace[parentNode] = (oldNode, newNode) =>
                                        {
                                            return oldNode.WithoutTrivia()
                                                          .WithLeadingTrivia(oldNode.GetLeadingTrivia())
                                                          .WithTrailingTrivia(oldNode.GetTrailingTrivia()
                                                                                     .Add(SyntaxFactory.CarriageReturnLineFeed)
                                                                                     .Add(SyntaxFactory.Whitespace(originalLineIndent + IndentAligner.Aligner.singleIndent)));
                                        };
                                    }
                                }
                            }
                            catch { }
                        });

                    // must be done in a single hit in order to avoid invalidating tokens during the run
                    root = root.ReplaceNodes(nodesToReplace.Keys, (oldNode, newNode) =>
                    {
                        return nodesToReplace[oldNode](oldNode, newNode);
                    });
                }
                catch { }
            return root;
        }
    }
}