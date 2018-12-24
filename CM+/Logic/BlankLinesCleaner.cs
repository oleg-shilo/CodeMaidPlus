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
    public static class BlankLinesCleaner
    {
        public static SyntaxNode RemoveXmlDocGaps(this SyntaxNode root)
        {
            // `SingleLineDocumentationCommentTrivia` points to the end of the comment block
            var commentsEnds = root.DescendantNodesAndTokens(null, true)
                                   .Where(x => x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                                   .Select(node => node.GetLocation().GetLineSpan().EndLinePosition.Line);

            var trtiviasToRemove = new List<SyntaxTrivia>();

            var trailinEmptyLines = new List<int>();

            foreach (var lineAfterComment in commentsEnds)
            {
                // if (root.GetLineText(lineAfterComment).HasNoText())
                for (int i = lineAfterComment; i < root.GetText().Lines.Count; i++)
                {
                    if (root.GetLineText(i).HasNoText())
                        trailinEmptyLines.Add(i);
                    else
                        break;
                }
            }

            // need to reverse so trivias removed from the bottom to top so the line numbering is not affected
            foreach (var line in trailinEmptyLines.OrderByDescending(x => x))
            {
                if (root.GetLineText(line).HasNoText())
                    try
                    {
                        var span = root.GetText().Lines[line].Span;

                        var trivia = root.DescendantTrivia(null, true)
                                         .FirstOrDefault(x => x.Span.IntersectsWith(span) &&
                                                              x.ToString().HasNoText());

                        root = root.ReplaceToken(trivia.Token,
                                                 trivia.Token.RemoveFromLeadingTrivia(trivia));
                    }
                    catch { } // failing formatting operations is not critical
            }

            return root;
        }
    }
}