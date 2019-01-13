using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
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
using Task = System.Threading.Tasks.Task;
using Document = Microsoft.CodeAnalysis.Document;
using Solution = Microsoft.CodeAnalysis.Solution;
using DteDocument = EnvDTE.Document;
using Newtonsoft.Json;

namespace CMPlus
{
    public class Settings
    {
        public bool SortUsings { get; set; } = true;
        public bool RemoveXmlDocGaps { get; set; } = true;
        public bool AlignIndents { get; set; } = true;
    }

    public static class CMSettings
    {
        static string configFile = Environment.SpecialFolder.LocalApplicationData.PathCombine("CodeMaid", "CodeMaid.config");
        static string configValueName = "ThirdParty_OtherCleaningCommandsExpression";

        public static bool IsCmInstalled { get => File.Exists(configFile); }

        static string CurrentIntegrationSetting
        {
            get
            {
                return (XDocument.Load(configFile)
                                 .Root
                                 .Descendants("setting")
                                 .FirstOrDefault(x => x.Attribute("name").Value == configValueName)
                                 ?.Element("value")
                                 .Value) ?? "";
            }

            set
            {
                var doc = XDocument.Load(configFile);

                var el = doc.Root
                            .Descendants("setting")
                            .FirstOrDefault(x => x.Attribute("name").Value == configValueName);

                if (el == null)
                {
                    el = new XElement("setting",
                             new XAttribute("name", configValueName),
                             new XAttribute("serializeAs", "String"),
                             new XElement("value", value));

                    doc.Root
                       .Descendants("setting")
                       .First()
                       .Parent
                       .Add(el);
                }
                else
                {
                    el.Element("value").SetValue(value);
                }

                doc.Save(configFile);
            }
        }

        public static bool IsIntegrated
        {
            get
            {
                try
                {
                    if (IsCmInstalled)
                    {
                        var value = CurrentIntegrationSetting;
                        return value.Contains(FormatCommand.CommandStrId);
                    }
                }
                catch { }
                return false;
            }
        }

        public static void ToggleIntegration()
        {
            if (IsCmInstalled)
            {
                string newValue;

                if (IsIntegrated)
                {
                    // remove CommandStrId
                    var commands = CurrentIntegrationSetting.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    newValue = string.Join("||", commands.Where(x => x != FormatCommand.CommandStrId)
                                                         .ToArray());
                }
                else
                {
                    // add CommandStrId
                    var commands = CurrentIntegrationSetting.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    newValue = string.Join("||", commands.Where(x => x != FormatCommand.CommandStrId)
                                                         .Concat(new[] { FormatCommand.CommandStrId })
                                                         .ToArray());
                }

                CurrentIntegrationSetting = newValue;
            }
        }
    }
}