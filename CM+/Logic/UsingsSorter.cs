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
        public static SyntaxNode SortUsings(this SyntaxNode root)
        {
            if (Runtime.Settings.SortUsings)
            {
                var usingNodes = root.DescendantNodesAndSelf(node => !node.IsKind(SyntaxKind.ClassDeclaration))
                                     .Where(node => node.IsKind(SyntaxKind.CompilationUnit) ||
                                                    node.IsKind(SyntaxKind.NamespaceDeclaration));

                root = root.ReplaceNodes(usingNodes, (originalNode, newNode) =>
                                                      {
                                                          var rawUsings = originalNode.GetUsings();
                                                          var orderedUsings = rawUsings.Sort();
                                                          return newNode.WithUsings(orderedUsings);
                                                      });
            }

            return root;
        }
    }
}