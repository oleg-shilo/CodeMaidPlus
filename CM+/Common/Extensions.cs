using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CMPlus
{
    public static class Extensions
    {
        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> list, Func<T, object> keySelector)
            => list.GroupBy(keySelector)
                   .Select(x => x.First());

        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        public static T CastTo<T>(this object obj) => (T)obj;

        public static bool IsEmpty<TSource>(this IEnumerable<TSource> source) => source != null ? !source.Any() : true;

        public static bool Contains(this string text, string pattern, int position)
        {
            if (text != null && position >= 0 && text.Length > position + pattern.Length)
                return (text.Substring(position, pattern.Length) == pattern);
            return false;
        }

        public static bool IsWhiteSpace(this string text, int index) => char.IsWhiteSpace(text[index]);

        public static bool EndsWith(this string text, int pos, string pattern)
        {
            return (pos >= pattern.Length && text.Contains(pattern, pos - pattern.Length));
        }

        // processing only whitespaces
        public static string SetIndentLength(this string text, int indentLength)
            => new string(' ', indentLength) + text.TrimStart();

        public static int GetIndentLength(this string text) => text.TakeWhile(x => x == ' ' || x == '\t').Count();

        public static bool HasNoText(this string text) => string.IsNullOrWhiteSpace(text);

        public static string FromCamelToPhrase(this string text)
        {
            return Regex.Replace(text, "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)", " $1", RegexOptions.Compiled);
            // return Regex.Replace(input, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
            //             string[] words = Regex.Matches("AlignIndents", "(^[a-z]+|[A-Z]+(?![a-z])|[A-Z][a-z]+)")
            // .OfType<Match>()
            // .Select(m => m.Value)
            // .ToArray();
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
            return collection;
        }

        public static bool HasText(this string text) => !string.IsNullOrWhiteSpace(text);

        public static T FindParent<T>(this DependencyObject obj) where T : DependencyObject
        {
            DependencyObject parent = obj;
            do
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            while (parent != null && !(parent is T));

            return parent as T;
        }

        public static bool HasCharAt(this string text, char pattern, int pos)
        {
            return (pos > 0 && pos < text.Length && text[pos] == pattern);
        }

        public static string PathCombine(this Environment.SpecialFolder folder, params string[] paths)
        {
            var parts = paths.ToList();
            parts.Insert(0, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            return Path.Combine(parts.ToArray());
        }

        public static string EnsureDirExis(this string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string[] GetLines(this string text)
            => text.Replace("\r\n", "\n").Split('\n');

        public static string GetDidName(this string path) => Path.GetDirectoryName(path);
    }

    public static class RoslynExtensions
    {
        public static SyntaxNode GetSyntaxRoot(this string code)
            => CSharpSyntaxTree.ParseText(code).GetRoot();

        public static string GetLineText(this SyntaxNode node, int line)
            => node.GetText().Lines[line].ToString();

        public static int GetLineNumber(this SyntaxToken token)
            => token.GetLocation().GetLineSpan().StartLinePosition.Line;

        public static int GetLineNumber(this SyntaxNode token)
            => token.GetLocation().GetLineSpan().StartLinePosition.Line;

        public static int GetStartLineNumber(this SyntaxNode node)
            => node.GetLocation().GetLineSpan().StartLinePosition.Line;

        public static int StartLinePositionCharacter(this SyntaxToken token)
            => token.GetLocation().GetLineSpan().StartLinePosition.Character;

        public static int EndLinePositionCharacter(this SyntaxToken token)
            => token.GetLocation().GetLineSpan().EndLinePosition.Character;

        public static int EndLinePositionCharacter(this SyntaxNode token)
            => token.GetLocation().GetLineSpan().EndLinePosition.Character;

        public static string Join(this IEnumerable<char> chars)
            => new string(chars.ToArray());

        public static string Multiply(this char @char, int times)
            => new string(@char, times);

        public static T Last<T>(this IEnumerable<T> items, int offset)
            => items.Reverse().Skip(offset).Take(1).First();

        public static T LastOrDefault<T>(this IEnumerable<T> items, int offset)
        {
            return items.Reverse().Skip(offset).Take(1).FirstOrDefault();
        }

        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            var index = 0;
            foreach (var item in source)
            {
                if (predicate.Invoke(item))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }

        public static IEnumerable<T> TakeAfter<T>(this IEnumerable<T> items, T item)
            => items.SkipWhile(x => !x.Equals(item))
                    .Skip(1);

        public static IEnumerable<SyntaxToken> TokensOfTheSameLine(this SyntaxToken token)
        {
            // instead of going through the ALL tokens go only through the tokens from the same parent
            // and within the same line

            var lineNumber = token.GetLineNumber();

            var tokens = token.SyntaxTree.GetRoot().DescendantTokens()
                             .Where(x => x.GetLineNumber() == lineNumber)
                             .ToArray();

            var parent = token.Parent;

            while (parent != null)
            {
                parent = parent.Parent;
                if (parent.GetFirstToken().GetLineNumber() != lineNumber)
                    break;
            }

            return parent.DescendantTokens()
                         .SkipWhile(x => x.GetLineNumber() != lineNumber)
                         .TakeWhile(x => x.GetLineNumber() == lineNumber)
                         .ToArray();
        }

        // public static SyntaxToken PrevLineStartToken(this SyntaxToken token)
        // {
        //     token.SyntaxTree.Select()
        //     // => token.GetLocation().GetLineSpan().StartLinePosition.Character;
        // }

        public static SyntaxToken? GetTokenFromPosition(this SyntaxNode root, int position)
        {
            try
            {
                return root.FindToken(position, true);
            }
            catch { return null; }
        }

        public static IEnumerable<UsingDirectiveSyntax> Sort(this IEnumerable<UsingDirectiveSyntax> source)
        {
            // System.*
            // Microsoft.*
            // <aliases>
            // <statics>
            return source.Select(x =>
                                 new
                                 {
                                     Syntax = x,
                                     Content = x.Name.ToString(),
                                     Hash = $"{x.Alias}{x.Name}".GetHashCode()
                                 })
                         .DistinctBy(x => x.Hash)
                         .OrderByDescending(x => x.Syntax.Alias == null)
                         .ThenByDescending(x => x.Syntax.StaticKeyword.ValueText == null)
                         .ThenByDescending(x => x.Content.StartsWith("System"))
                         .ThenByDescending(x => x.Content.StartsWith("Microsoft"))
                         .ThenBy(x => x.Content)
                         .Select(x => x.Syntax)
                         .ToArray();
        }

        public static int EndLineNumber(this SyntaxToken node)
            => node.GetLocation().GetLineSpan().EndLinePosition.Line;

        public static SyntaxTrivia LastWhitespaceTrivia(this SyntaxToken token)
            => token.LeadingTrivia.LastOrDefault(x => x.IsKind(SyntaxKind.WhitespaceTrivia));

        public static SyntaxTrivia? ParentStatementWhitespaceTrivia(this SyntaxToken token)
            => token.ParentStatement()?.GetLeadingTrivia().LastOrDefault(x => x.IsKind(SyntaxKind.WhitespaceTrivia));

        public static bool IsPartOfInterpolationExpression(this SyntaxToken token)
        {
            var result = token.Parent;
            do
            {
                if (result.IsKind(SyntaxKind.InterpolatedStringExpression))
                    return true;

                result = result.Parent;
            }
            while (result != null);

            return false;
        }

        public static bool IsValidIndentCandidateToken(this SyntaxToken token)
        {
            if (token.IsStringContentToken())
            {
                if (token.IsKind(SyntaxKind.InterpolatedStringStartToken) ||
                    token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken))
                    return true;
                else
                    return false;
            }
            else
            {
                if (Runtime.Settings.DoNotAlignInterpolation)
                    return !token.IsPartOfInterpolationExpression();
                else
                    return true;
            }
        }

        public static bool IsStringContentToken(this SyntaxToken token)
            => token.IsKind(SyntaxKind.InterpolatedStringToken) ||
               token.IsKind(SyntaxKind.InterpolatedStringTextToken) ||
               token.IsKind(SyntaxKind.InterpolatedStringStartToken) ||
               token.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken) ||
               token.IsKind(SyntaxKind.StringLiteralToken);

        public static SyntaxToken GetParentChildToken(this SyntaxToken token, SyntaxKind kind)
            => token.Parent.ChildTokens()
                           .FirstOrDefault(x => x.IsKind(kind));

        public static SyntaxNode ParentStatement(this SyntaxToken token)
        {
            var result = token.Parent;
            do
            {
                if (result is StatementSyntax)
                    return result;
                result = result.Parent;
            }
            while (result != null);

            return result;
        }

        public static SyntaxNode BrakeStringExpression(this SyntaxNode expression, int breakLength, string newLineIndent)
        {
            var text = expression.ToFullString();
            var left = text.Substring(0, breakLength);
            var right = text.Substring(breakLength);
            var preffix = left.Split('"')[0];
            var newLine = "\r\n";

            return SyntaxFactory.ParseExpression($"{left}\"{newLine}{newLineIndent}+ {preffix}\"{right}");
        }

        public static SyntaxNode ParentNode(this SyntaxToken token, Predicate<SyntaxNode> selector)
        {
            var result = token.Parent;
            do
            {
                if (selector(result))
                    return result;
                result = result.Parent;
            }
            while (result != null);

            return result;
        }

        public static SyntaxToken RemoveFromLeadingTrivia(this SyntaxToken node, SyntaxTrivia trivia)
            => node.WithoutTrivia()
                   .WithLeadingTrivia(node.LeadingTrivia.Remove(trivia))
                   .WithTrailingTrivia(node.TrailingTrivia);

        public static SyntaxNode WithUsings(this SyntaxNode node, IEnumerable<UsingDirectiveSyntax> usings)
            => (SyntaxNode)(node as CompilationUnitSyntax)?.WithUsings(SyntaxFactory.List(usings)) ??
                (SyntaxNode)(node as NamespaceDeclarationSyntax)?.WithUsings(SyntaxFactory.List(usings));

        public static IEnumerable<UsingDirectiveSyntax> GetUsings(this SyntaxNode node)
            => (node as CompilationUnitSyntax)?.Usings ??
                (node as NamespaceDeclarationSyntax)?.Usings;
    }
}