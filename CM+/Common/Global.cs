using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Document = Microsoft.CodeAnalysis.Document;
using DteDocument = EnvDTE.Document;
using Solution = Microsoft.CodeAnalysis.Solution;
using Task = System.Threading.Tasks.Task;

namespace CMPlus
{
    public static class Global
    {
        static public AsyncPackage Package;

        static public T GetService<T>()
            => (T)Package?.GetServiceAsync(typeof(T))?.Result;

        static public DteDocument GetActiveDteDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            dynamic dte = GetService<EnvDTE.DTE>();
            return (DteDocument)dte.ActiveDocument;
        }

        public static Document GetActiveDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Solution solution = Workspace.CurrentSolution;
            string activeDocPath = GetActiveDteDocument()?.FullName;

            if (activeDocPath != null)
                return solution.Projects
                               .SelectMany(x => x.Documents)
                               .FirstOrDefault(x => x.SupportsSyntaxTree &&
                                                    x.SupportsSemanticModel &&
                                                    x.FilePath == activeDocPath);
            return null;
        }

        static VisualStudioWorkspace workspace = null;

        static public VisualStudioWorkspace Workspace
        {
            get
            {
                if (workspace == null)
                {
                    IComponentModel componentModel = GetService<SComponentModel>() as IComponentModel;
                    workspace = componentModel.GetService<VisualStudioWorkspace>();
                }
                return workspace;
            }
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

        public static SyntaxTrivia ParentStatementWhitespaceTrivia(this SyntaxToken token)
            => token.ParentStatement().GetLeadingTrivia().LastOrDefault(x => x.IsKind(SyntaxKind.WhitespaceTrivia));

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

        public static string GetLineText(this SyntaxNode node, int line)
            => node.GetText().Lines[line].ToString();

        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> list, Func<T, object> keySelector)
            => list.GroupBy(keySelector)
                   .Select(x => x.First());

        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text);

        public static bool HasNoText(this string text) => string.IsNullOrWhiteSpace(text);
    }

    static class AttachedProperies
    {
        public static ConditionalWeakTable<object, Dictionary<string, object>> ObjectCache
            = new ConditionalWeakTable<object, Dictionary<string, object>>();

        public static T SetValue<T>(this T obj, string name, object value) where T : class
        {
            Dictionary<string, object> properties = ObjectCache.GetOrCreateValue(obj);

            if (properties.ContainsKey(name))
                properties[name] = value;
            else
                properties.Add(name, value);

            return obj;
        }

        public static T GetValue<T>(this object obj, string name)
        {
            Dictionary<string, object> properties;
            if (ObjectCache.TryGetValue(obj, out properties) && properties.ContainsKey(name))
                return (T)properties[name];
            else
                return default(T);
        }

        public static object GetValue(this object obj, string name)
        {
            return obj.GetValue<object>(name);
        }
    }
}