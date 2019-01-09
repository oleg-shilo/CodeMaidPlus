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
            => new Aligner().AlignIndents(root);

        class Aligner
        {
            static string singleIndent = "    ";

            IEnumerable<int> IndentPoints = new List<int>();

            string VisualizeIndentPoints()
            {
                if (!IndentPoints.Any())
                    return "";

                var text = new char[IndentPoints.Max() + 1];
                foreach (int point in IndentPoints)
                    text[point] = '^';
                return new string(text);
            }

            void UpdateIndentPointsFrom(string line)
            {
                IndentPoints = IndentPoints.Where(x => x <= line.GetIndentLength())
                                           .Concat(FindIndentPoints(line))
                                           .OrderBy(x => x)
                                           .ToArray();

                // if (prevLine.TrimEnd().EndsWith(";") || // prev statement is completed
                //     prevLine.TrimEnd().EndsWith("}") || // prev statement is completed
                //     prevLine.TrimEnd().EndsWith(")") || // prev statement is completed
                //     line.TrimStart().StartsWith("{") || // current statement is a block start
                //     line.TrimStart().StartsWith("}") || // current statement is a block end
                //     prevLine.TrimEnd().EndsWith("{")
                //     )
                // {
                //     for (int i = 0; i < prevLine.GetIndentLength() / singleIndent.Length + 1; i++)
                //     // for (int i = 0; i < prevLine?.Length / singleIndent.Length + 1; i++) // TBD
                //     {
                //         indentPoints.Add(singleIndent.Length * (i + 1));
                //     }
                // }
            }

            public SyntaxNode AlignIndents(SyntaxNode root)
            {
                string prevNonEmptyLine = null;
                var lineInfo = new Dictionary<SyntaxToken, (SyntaxTrivia trivia, int line, int indentDelta)>();

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

                            indent = opening.StartLinePositionCharacter();

                            // check if line was already adjusted and apply adjustment delta
                            var info = lineInfo.Values.FirstOrDefault(x => x.line == opening.GetLineNumber());

                            if (info.line != 0) // `0` means not found as `info` is a tuple
                                indent -= info.indentDelta;
                        }
                        return success;
                    }

                    var lineNumber = startToken.GetLineNumber();

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
                            UpdateIndentPointsFrom(prevNonEmptyLine);

                            lineInfo.Add(startToken, (SyntaxFactory.Whitespace(newItemIndent),
                                                      lineNumber,
                                                      currentIndent - bestIndent));

                            Console.WriteLine(prevNonEmptyLine);
                            Console.WriteLine(VisualizeIndentPoints());
                            continue;
                        }
                    }

                    prevNonEmptyLine = text;
                    UpdateIndentPointsFrom(prevNonEmptyLine);

                    Console.WriteLine(prevNonEmptyLine);
                    Console.WriteLine(VisualizeIndentPoints());
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

                // if (prevLine.TrimEnd().EndsWith(";") || // prev statement is completed
                //     prevLine.TrimEnd().EndsWith("}") || // prev statement is completed
                //     prevLine.TrimEnd().EndsWith(")") || // prev statement is completed
                //     line.TrimStart().StartsWith("{") || // current statement is a block start
                //     line.TrimStart().StartsWith("}") || // current statement is a block end
                //     prevLine.TrimEnd().EndsWith("{")
                //     )
                // {
                //     for (int i = 0; i < prevLine.GetIndentLength() / singleIndent.Length + 1; i++)
                //     // for (int i = 0; i < prevLine?.Length / singleIndent.Length + 1; i++) // TBD
                //     {
                //         indentPoints.Add(singleIndent.Length * (i + 1));
                //     }
                // }

                indentPoints.AddRange(IndentPoints);

                // else
                //     for (int i = 0; i < prevLine?.Length / singleIndent.Length + 1; i++) // TBD
                //     {
                //         indentPoints.Add(singleIndent.Length * (i + 1));
                //     }

                indentPoints.RemoveAll(x => x > prevLine.GetIndentLength() + singleIndent.Length);
                if (prevLine.TrimEnd().EndsWith("("))// prev statement is completed
                    indentPoints.Add(prevLine.TrimEnd().Length);

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

            IEnumerable<int> FindIndentPoints(string text)
            {
                text = text.TrimEnd();

                var indentPoints = new List<int>();

                indentPoints.Add(text.GetIndentLength());

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '.')
                    {
                        // capture all de-referencings
                        indentPoints.Add(i);
                    }
                    else if (text[i] == '(')
                    {
                        // the first position of the argument (in the future can be a part of "arg list alignment")
                        indentPoints.Add(i + 1);
                    }
                    else if (text.Contains("=> ", i - 3) || text.Contains("= ", i - 2))
                    {
                        // the position of the first element in the lambs expression
                        indentPoints.Add(i);
                    }
                    else if (text[i] == '{')
                    {
                        // if (i != text.Length - 1)
                        // {
                        //     // the first indent of the block
                        //     indentPoints.Add(i);
                        // }
                        indentPoints.Remove(i);
                        // and the next indent in the block
                        indentPoints.Add(i + singleIndent.Length);
                    }
                }

                return indentPoints;
            }
        }
    }
}