using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Threading;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Solution = Microsoft.CodeAnalysis.Solution;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Document = Microsoft.CodeAnalysis.Document;
using DteDocument = EnvDTE.Document;
using Task = System.Threading.Tasks.Task;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Xml.Linq;

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

        private static VisualStudioWorkspace workspace = null;

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
    }
}