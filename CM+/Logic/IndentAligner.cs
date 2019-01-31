using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CMPlus
{
    public static class IndentAligner
    {
        public static SyntaxNode AlignIndents(this SyntaxNode root, Action<int, string> onLineVisualized = null)
        {
            if (Runtime.Settings.AlignIndents)
                try
                {
                    root = new Aligner(onLineVisualized).AlignIndents(root);
                }
                catch { }
            return root;
        }

        class Aligner
        {
            static string singleIndent = "    ";
            Action<int, string> onLineVisualized;
            IEnumerable<int> IndentPoints = new List<int>();

            public Aligner(Action<int, string> onLineVisualized = null)
            {
                this.onLineVisualized = onLineVisualized;
            }

            string VisualizeIndentPoints()
            {
                if (!IndentPoints.Any())
                    return "";

                var text = new char[IndentPoints.Max() + 1];
                foreach (int point in IndentPoints)
                    text[point] = '^';
                return new string(text);
            }

            string MergedIndentPointsLine = "";

            void UpdateIndentPointsFrom(string line, SyntaxToken lineStartToken)
            {
                if (IndentPoints.IsEmpty() && line.IsEmpty())
                {
                    IndentPoints = new[] { 0, singleIndent.Length };
                }
                else
                {
                    if (MergedIndentPointsLine.IsEmpty())
                    {
                        MergedIndentPointsLine = line;
                    }
                    else
                    {
                        var chars = line.ToCharArray();
                        for (int i = 0; i < chars.Length; i++)
                        {
                            if (char.IsWhiteSpace(chars[i]) && MergedIndentPointsLine.Length > i)
                                chars[i] = MergedIndentPointsLine[i];
                        }
                        MergedIndentPointsLine = new string(chars);
                    }

                    IndentPoints = IndentPoints.Where(x => x < line.GetIndentLength())
                                               .Concat(FindIndentPoints(line, lineStartToken))
                                               .Distinct()
                                               .OrderBy(x => x)
                                               .ToArray();
                }
            }

            void Output(int lineNumber, string line)
            {
                if (onLineVisualized != null)
                {
                    onLineVisualized(lineNumber, line);
                    onLineVisualized(lineNumber, VisualizeIndentPoints());
                }
            }

            public SyntaxNode AlignIndents(SyntaxNode root)
            {
                string prevNonEmptyLine = null;
                var lineInfo = new Dictionary<SyntaxToken, (SyntaxTrivia trivia, int line, int indentDelta)>();
                var blockDeltas = new Dictionary<int, (int size, bool isExact)>();

                var linesWithCode = root.DescendantTokens()
                                        .Where(x => x.HasLeadingTrivia &&
                                                    x.LeadingTrivia.Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)));

                if (Runtime.Settings.DoNotAlignInterpolation)
                    linesWithCode = linesWithCode.Where(x => !x.Parent.IsKind(SyntaxKind.Interpolation));

                var lines = root.GetText().Lines;

                SyntaxToken prevStartToken = root.GetFirstToken();

                foreach (SyntaxToken startToken in linesWithCode)
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

                            indent = opening.StartLinePositionCharacter();

                            // check if line was already adjusted and apply adjustment delta
                            var info = lineInfo.Values.FirstOrDefault(x => x.line == opening.GetLineNumber());

                            if (info.line != 0) // `0` means not found as `info` is a tuple
                                indent -= info.indentDelta;
                        }
                        return success;
                    }

                    var lineNumber = startToken.GetLineNumber();

                    if (lineNumber == 0)
                        continue;

                    var text = lines[lineNumber].ToString();

                    if (IndentPoints.IsEmpty())
                    {
                        if (prevNonEmptyLine.IsEmpty() && lines.Count > lineNumber)
                        {
                            prevStartToken = root.DescendantTokens().FirstOrDefault(x => x.GetLineNumber() == lineNumber - 1);
                            prevNonEmptyLine = (lines[lineNumber - 1].ToString());
                        }

                        UpdateIndentPointsFrom(prevNonEmptyLine, prevStartToken);
                        Output(lineNumber - 1, prevNonEmptyLine);
                    }

                    if (text.HasText())
                    {
                        bool lookForBestAlignment = true;

                        if (Runtime.Settings.DoNotAlignInterpolation && startToken.IsPartOfInterpolationExpression())
                            lookForBestAlignment = false;

                        int currentIndent = text.GetIndentLength();

                        if (currentIndent == 0)
                            continue; // start of the line, no need to align

                        int bestIndent = currentIndent;

                        // startToken is a closing token, which has matching opening token
                        // on a different line. This is because the closing token is the first token in the line.
                        var processedAsBlockContent = false;
                        if (lookForBestAlignment)
                        {
                            if (MatchingOpenTokenIndentFor(SyntaxKind.CloseBraceToken, out int indent))
                            {
                                bestIndent = indent;
                                processedAsBlockContent = true;
                            }
                            else if (MatchingOpenTokenIndentFor(SyntaxKind.CloseParenToken, out indent))
                            {
                                bestIndent = indent;
                                processedAsBlockContent = true;
                            }
                        }

                        if (!processedAsBlockContent)
                        {
                            if (blockDeltas.ContainsKey(lineNumber))
                            {
                                var delta = blockDeltas[lineNumber];
                                int blockAdjustedCurrentIndent = currentIndent - delta.size;

                                if (delta.isExact)
                                {
                                    bestIndent = blockAdjustedCurrentIndent;
                                }
                                else
                                {
                                    if (lookForBestAlignment)
                                        bestIndent = FindBestIndentAlignment(blockAdjustedCurrentIndent,
                                                                             text.SetIndentLength(blockAdjustedCurrentIndent),
                                                                             prevNonEmptyLine);
                                }
                            }
                            else
                            {
                                if (lookForBestAlignment)
                                    bestIndent = FindBestIndentAlignment(currentIndent, text, prevNonEmptyLine);
                            }
                        }

                        if ((startToken.IsKind(SyntaxKind.OpenBraceToken) || startToken.IsKind(SyntaxKind.OpenParenToken))
                            && !startToken.IsPartOfInterpolationExpression())
                        {
                            var node = startToken.Parent;
                            var blockLines = node.ChildNodes()
                                                 .Select(x =>
                                                 {
                                                     var index = x.GetStartLineNumber();
                                                     var txt = lines[index].ToString();
                                                     return new
                                                     {
                                                         index,
                                                         text = txt,
                                                         indent = txt.GetIndentLength()
                                                     };
                                                 });

                            if (blockLines.Any()) // it can be empty block like in empty try..catch statement
                            {
                                int prevLineDelta = 0;
                                for (int i = blockLines.Min(x => x.index); i <= blockLines.Max(x => x.index); i++)
                                {
                                    (int size, bool isExact) delta = (0, false);
                                    var info = blockLines.FirstOrDefault(x => x.index == i);
                                    if (info != null)
                                    {
                                        var nextLineDesiredIndent = bestIndent + singleIndent.Length;
                                        delta = (info.indent - nextLineDesiredIndent, true);
                                    }
                                    else
                                        delta = (prevLineDelta, false);

                                    if (blockDeltas.ContainsKey(i) && !delta.isExact)
                                    {
                                        var oldDelta = blockDeltas[i];
                                        blockDeltas[i] = (oldDelta.size + delta.size, false);
                                    }
                                    else
                                    {
                                        blockDeltas[i] = delta;
                                    }
                                    prevLineDelta = delta.Item1;
                                }
                            }
                        }

                        if (currentIndent != bestIndent)
                        {
                            var newItemIndent = new string(' ', bestIndent);

                            prevNonEmptyLine = newItemIndent + text.TrimStart();
                            prevStartToken = startToken;

                            UpdateIndentPointsFrom(prevNonEmptyLine, prevStartToken);

                            lineInfo.Add(startToken, (SyntaxFactory.Whitespace(newItemIndent),
                                                      lineNumber,
                                                      currentIndent - bestIndent));

                            Output(lineNumber, prevNonEmptyLine);
                            continue;
                        }
                    }

                    prevNonEmptyLine = text;
                    prevStartToken = startToken;
                    UpdateIndentPointsFrom(prevNonEmptyLine, prevStartToken);

                    Output(lineNumber, prevNonEmptyLine);
                }

                // must be done in a single hit in order to avoid invalidating tokens during the run
                root = root.ReplaceTokens(lineInfo.Keys,
                    (oldToken, newToken) =>
                    {
                        var newWhitespace = lineInfo[oldToken].trivia;
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

            int FindBestIndentAlignment(int currentIndent, string line, string prevLine)
            {
                var startsWithDot = line.TrimStart().StartsWith(".");

                if (IndentPoints.IsEmpty())
                    return currentIndent;

                if (IndentPoints.Last() <= currentIndent)
                    return IndentPoints.Last();

                if (currentIndent <= IndentPoints.First())
                    return IndentPoints.First();

                // if we are here then currentIndent is between  IndentPoints.First and IndentPoints.Last
                var pointsBefore = IndentPoints.TakeWhile(x => x < currentIndent);

                var pointBefore = pointsBefore.Last();
                var pointAfter = IndentPoints.ToArray()[pointsBefore.Count()];

                // if (prevLine.HasCharAt('.', pointBefore - 1))
                if (MergedIndentPointsLine.HasCharAt('.', pointBefore - 1))
                {
                    if (startsWithDot)
                        pointBefore--;
                }

                // if (prevLine.HasCharAt('.', pointAfter - 1))
                if (MergedIndentPointsLine.HasCharAt('.', pointAfter - 1))
                {
                    if (startsWithDot)
                        pointAfter--;
                }

                if ((currentIndent - pointBefore) >= (pointAfter - currentIndent))
                    return pointAfter;
                else
                    return pointBefore;
            }

            IEnumerable<int> FindIndentPoints(string text, SyntaxToken startToken)
            {
                SyntaxNode root = startToken.SyntaxTree?.GetRoot();

                var rawIndent = startToken.StartLinePositionCharacter();
                var actualIndent = text.GetIndentLength();

                text = text.TrimEnd();

                var indentPoints = new List<int>();

                indentPoints.Add(text.GetIndentLength());
                indentPoints.Add(text.GetIndentLength() + singleIndent.Length);

                int pointToPosition(int point)
                {
                    var indentDelta = actualIndent - rawIndent;
                    return (point - actualIndent) - indentDelta + startToken.Span.Start;
                }

                for (int i = 0; i < text.Length; i++)
                {
                    bool isValid()
                    {
                        if (startToken.SyntaxTree != null)
                        {
                            // verify that the points are not part of a string
                            // position          500
                            // raw               5
                            // actual    0     4      8
                            //           .     . |    .
                            //                 n e w  E x c...
                            var pos = pointToPosition(i);

                            if (root.GetTokenFromPosition(pos)?.IsValidIndentCandidateToken() == false)
                                return false;
                        }
                        return true;
                    }

                    if (text[i] == '.')
                    {
                        // capture all de-referencings
                        if (isValid())
                            indentPoints.Add(i + 1);
                    }
                    else if (text[i] == '(')
                    {
                        // the first position of the argument (in the future can be a part of "arg list alignment")
                        if (isValid())
                            indentPoints.Add(i + 1);
                    }
                    else if (text.EndsWith(i, "$\""))
                    {
                        indentPoints.Add(i - 1);
                    }
                    else if (text.EndsWith(i, "=> ") ||
                             text.EndsWith(i, "= ") ||
                             text.EndsWith(i, "return ") ||
                             text.EndsWith(i, ": ") ||
                             (i >= 1 && text.IsWhiteSpace(i - 1) &&
                                            !text.IsWhiteSpace(i) &&
                                            text[i] != '='))
                    {
                        // the position of the first element in the lambs expression
                        if (isValid())
                        {
                            indentPoints.Add(i);
                            indentPoints.Add(i + singleIndent.Length); // remove if becomes too noisy
                        }
                    }
                    if (text.EndsWith("("))
                    {
                        // prev statement is completed
                        if (isValid())
                            indentPoints.Add(text.Length);
                    }
                    else if (text[i] == '{')
                    {
                        if (isValid())
                        {
                            if (i != text.Length - 1)
                            {
                                // the first indent of the block
                                indentPoints.Add(i);
                            }

                            // should not remove as it will create problem with dictionary declaration:
                            // new Dictionary<int, int>
                            // {
                            //     { 1, 2},
                            //     { 3, 4}
                            // }

                            // and the next indent in the block
                            indentPoints.Add(i + singleIndent.Length);
                        }
                        else
                        {
                        }
                    }
                }

                // if (startToken.SyntaxTree != null)
                // {
                //     foreach (int i in indentPoints.ToArray())
                //     {
                //         int pointPosition = (i - actualIndent) - indentDelta + startToken.Span.Start;
                //         var tokenOfPoint = root.GetTokenFromPosition(pointPosition - 1);

                //         // still allow start content
                //         bool valid = tokenOfPoint.IsKind(SyntaxKind.InterpolatedStringStartToken) ||
                //                      tokenOfPoint.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken) ||
                //                      !tokenOfPoint.IsStringContentToken();

                //         if (!valid)
                //             indentPoints.Remove(i);
                //     }
                // }

                return indentPoints.Distinct();
            }
        }

        public class DecoratedView
        {
            public string code;
            Dictionary<int, string> changedLines = new Dictionary<int, string>();

            public DecoratedView(string unchangedCode)
            {
                this.code = unchangedCode;
            }

            public void OnLineChanged(int lineNumber, string lineText)
            {
                if (changedLines.ContainsKey(lineNumber))
                    changedLines[lineNumber] += Environment.NewLine + lineText;
                else
                    changedLines[lineNumber] = lineText;
            }

            public override string ToString()
            {
                var buffer = new StringBuilder();
                var rawLines = code.GetLines();
                for (int i = 0; i < rawLines.Length; i++)
                {
                    if (changedLines.ContainsKey(i))
                        buffer.AppendLine(changedLines[i]);
                    else
                        buffer.AppendLine(rawLines[i]);
                }

                return buffer.ToString().Replace("\0", " ");
            }
        }
    }
}