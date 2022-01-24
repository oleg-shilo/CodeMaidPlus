using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CMPlus
{
    /// <summary>
    /// Interaction logic for SettingsToolWindowControl.
    /// </summary>
    public partial class SettingslWindowControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingslWindowControl"/> class.
        /// </summary>
        public SettingslWindowControl()
        {
            this.InitializeComponent();
            this.Loaded += SettingsWindowControl_Loaded;
        }

        private object progressBar;

        public event PropertyChangedEventHandler PropertyChanged;

        public class SettingsItem
        {
            bool enabled;

            public bool Enabled
            {
                get
                {
                    return enabled;
                }

                set
                {
                    enabled = value;
                    typeof(Settings)
                        .GetProperties()
                        .Where(p => p.Name == this.Name)
                        .ForEach(p => p.SetValue(Runtime.Settings, value));
                }
            }

            public string Name { get; set; }
            public string Description { get; set; }
            public string ImageUri { get; set; }

            public override string ToString()
            {
                return Name?.FromCamelToPhrase();
            }
        }

        private void RefreshSettings()
        {
            this.Settings.Clear();
            typeof(Settings)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(bool) && p.CanRead && p.CanWrite)
                .Select(p => new SettingsItem
                {
                    Enabled = (bool)p.GetValue(Runtime.Settings),
                    Name = p.Name,
                    Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    ImageUri = p.GetCustomAttribute<ImageAttribute>()?.Uri,
                })
                .ForEach(this.Settings.Add);

            this.featureSelector.SelectedItem = Settings.FirstOrDefault();
        }

        public static DependencyProperty ImageBeforeProperty =
            DependencyProperty.Register(nameof(ImageBefore), typeof(string), typeof(SettingslWindowControl));

        public string ImageBefore
        {
            get => (string)GetValue(ImageBeforeProperty);
            set => SetValue(ImageBeforeProperty, value);
        }

        public string Version { get; set; } = "v" + typeof(SettingslWindowControl).Assembly.GetName().Version;

        public static DependencyProperty ImageAfterProperty =
            DependencyProperty.Register(nameof(ImageAfter), typeof(string), typeof(SettingslWindowControl));

        public string ImageAfter
        {
            get { return (string)GetValue(ImageAfterProperty); }
            set { SetValue(ImageAfterProperty, value); }
        }

        Window ParentWindow => this.FindParent<Window>();

        private void SettingsWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            var hostWindow = this.ParentWindow;
            if (hostWindow != null)
            {
                // there is no way to adjust window size from the designer
                // the initial size of hostWindow is defined by the Runtime.Settings.Window.* defaults

                hostWindow.Width = Runtime.Settings.WindowWidth;
                hostWindow.Height = Runtime.Settings.WindowHeight;

                hostWindow.Deactivated += (x, y) => Runtime.Settings.Save();

                hostWindow.Closing += (x, y) =>
                {
                    Runtime.Settings.WindowWidth = hostWindow.Width;
                    Runtime.Settings.WindowHeight = hostWindow.Height;
                    Runtime.Settings.Save();
                };
            }

            dirSelector.SelectedItem = null;
            RefreshStatus();
            RefreshSettings();

            DataContext = this;
        }

        public ObservableCollection<SettingsItem> Settings { get; set; } = new ObservableCollection<SettingsItem>();

        private void RefreshStatus()
        {
            integrate.IsEnabled = CMSettings.IsCmInstalled;

            if (CMSettings.IsIntegrated)
            {
                integrate.Content = "Unintegrate";
                status.Text = "Integrated";
            }
            else
            {
                integrate.Content = "Integrate";
                status.Text = "Unintegrated" + (CMSettings.IsCmInstalled ? "" : " (CodeMaid is not installed)");
            }
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // Excellent example of code analysis going crazy: disabling a simple warning with two extremely
            // noisy attributes for every warning!!!
        }

        private void integrate_Click(object sender, RoutedEventArgs e)
        {
            CMSettings.ToggleIntegration();
            RefreshStatus();

            this.ParentWindow?.Close();

            MessageBox.Show("The changes will take affect only when you restart Visual Studio or press 'Save' " +
                            "button in the CodeMaid 'Options' dialog.", "CM+ Settings");
        }

        private void help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/oleg-shilo/CodeMaidPlus");
            }
            catch { }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow?.Close();
        }

        private void processDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(selectedDir.Text))
                {
                    var utf8WithBom = new System.Text.UTF8Encoding(true);

                    var files = Directory.GetFiles(selectedDir.Text, "*.cs", SearchOption.AllDirectories);
                    var count = 0;

                    this.progress.Visibility = Visibility.Visible;
                    this.processDir.IsEnabled = false;
                    this.progress.Value = count;
                    this.progress.Maximum = files.Count();

                    Task.Run(() =>
                    {
                        foreach (var file in files)
                        {
                            var code = File.ReadAllText(file);

                            var root = code.GetSyntaxRoot();

                            var formattedCode = FormatCommand.Process(root).ToFullString();

                            if (code != formattedCode)
                                File.WriteAllText(file, formattedCode, utf8WithBom);

                            InUiThread(() => this.progress.Value = count++);
                        }

                        InUiThread(() =>
                        {
                            this.progress.Visibility = Visibility.Collapsed;
                            this.processDir.IsEnabled = true;
                        });
                    });
                }
                else
                    MessageBox.Show($"Directory '{selectedDir.Text}' does not exist.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        string description;

        public string Description
        {
            get => description;
            set { description = value; PropChangeNotify(nameof(Description)); }
        }

        string alignmentPreview;

        public string AlignmentPreview
        {
            get => alignmentPreview;
            set { alignmentPreview = value; PropChangeNotify(nameof(AlignmentPreview)); }
        }

        string alignmentInput = @"var lineScan = new LineScan
{
    LineNumber = text.LineNumber,
   TimeScaned = text.TimeLoaded,
    Scans = text.Positions
                 .Where(p => p.Span != null &&
                            p.State == LineState.Known)
                  .Select(region => new RegionScan
                    {
                        Position = region.Position,
                     Span = region.Span
                    })
};";

        public string AlignmentInput
        {
            get => alignmentInput;
            set { alignmentInput = value; PropChangeNotify(nameof(AlignmentInput)); }
        }

        void PropChangeNotify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        static void InUiThread(Action action) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dirSelector.SelectedItem != null)
            {
                dirSelector.SelectedItem = null;
                try
                {
                    selectedDir.Text = Path.GetDirectoryName(Global.Workspace.CurrentSolution.FilePath);
                }
                catch
                {
                    selectedDir.Text = "< error >";
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Description = featureSelector.SelectedItem?
                                         .CastTo<SettingsItem>()
                                         .Description;

            var image = featureSelector.SelectedItem?
                                       .CastTo<SettingsItem>()
                                           .ImageUri;

            // "/CM+;component/Resources/using.{when}.png";
            ImageBefore = image?.Replace("{when}", "before");
            ImageAfter = image?.Replace("{when}", "after");
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            var resultCode = new IndentAligner.DecoratedView(AlignmentInput);

            AlignmentInput.GetSyntaxRoot()
                          .AlignIndents(resultCode.OnLineChanged);

            AlignmentPreview = resultCode.ToString();
        }
    }
}