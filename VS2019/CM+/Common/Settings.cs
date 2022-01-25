using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;
using Document = Microsoft.CodeAnalysis.Document;
using Solution = Microsoft.CodeAnalysis.Solution;
using DteDocument = EnvDTE.Document;

namespace CMPlus
{
    class ImageAttribute : Attribute
    {
        public string Uri;

        public ImageAttribute(string uri) => Uri = uri;
    }

    public class Settings
    {
        [Image("/CM+;component/Resources/using.{when}.png")]
        [Description("All 'usings' are grouped and sorted alphabetically: using System.* | using Microsoft.* | " +
                     "using <aliases> | using <statics>")]
        public bool SortUsings { get; set; } = true;

        [Image("/CM+;component/Resources/doc.{when}.png")]
        [Description("Remove XML Documentation block trailing blank line.")]
        public bool RemoveXmlDocGaps { get; set; } = true;

        // [Image("/CM+;component/Resources/doc.{when}.png")]
        [Description("Trims code lines that exceed line length limit (within specified tolerance).")]
        public string TrimmLinesData { get; set; } = "120:20";

        public bool TrimmLines { get => TrimmLinesData.Any(); }

        internal int TrimmLinesLimit { get => int.Parse(TrimmLinesData.Split(':').First()); }
        internal int TrimmLinesTolerance { get => int.Parse(TrimmLinesData.Split(':').Last()); }

        [Image("/CM+;component/Resources/indent-1.{when}.png")]
        [Description("Align indents so the nearest logical anchor.")]
        public bool AlignIndents { get; set; } = true;

        [Image("/CM+;component/Resources/brackets.{when}.png")]
        [Description("Fix \"Egyptian brackets\" in cases that are not handled by CodeMaid.")]
        public bool FixBrackets { get; set; } = true;

        [Image("/CM+;component/Resources/interpolation.{when}.png")]
        [Description("Do not process code in the string interpolation expressions.")]
        public bool DoNotAlignInterpolation { get; set; } = true;

        public double WindowWidth { get; set; } = 950;
        public double WindowHeight { get; set; } = 550;
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