#region Licence...

/*
The MIT License (MIT)

Copyright (c) 2016 Oleg Shilo

*/

#endregion Licence...

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
    static public class UsingsSorter
    {
        /// <summary>
        /// Sorts the usings.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <returns></returns>
        public static SyntaxNode SortUsings(this SyntaxNode root)
        {
            if (Runtime.Settings.SortUsings)
                try
                {
                    var usingNodes = root.DescendantNodesAndSelf(node => !node.IsKind(SyntaxKind.ClassDeclaration))
                                         .Where(node => node.IsKind(SyntaxKind.CompilationUnit) ||
                                                        node.IsKind(SyntaxKind.NamespaceDeclaration));

                    if (usingNodes.Any())
                        root = root.ReplaceNodes(usingNodes,
                                   (originalNode, newNode) =>
                                   {
                                       var rawUsings = originalNode.GetUsings().ToArray();
                                       if (rawUsings.Any())
                                       {
                                           var topUsingTrivia = rawUsings[0].GetLeadingTrivia();
                                           rawUsings[0] = rawUsings[0].WithoutLeadingTrivia();

                                           var orderedUsings = rawUsings.Sort().ToArray();

                                           orderedUsings[0] = orderedUsings[0].WithLeadingTrivia(topUsingTrivia);
                                           return newNode.WithUsings(orderedUsings);
                                       }
                                       return originalNode;
                                   });
                }
                catch { }
            return root;
        }
    }
}