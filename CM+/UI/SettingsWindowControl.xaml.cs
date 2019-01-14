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
using System.Windows.Media;
using System.Windows.Threading;

namespace CMPlus
{
    /// <summary>
    /// Interaction logic for SettingsWindowControl.
    /// </summary>
    public partial class SettingsWindowControl : UserControl, INotifyPropertyChanged
    {
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

            public override string ToString()
            {
                return Name?.FromCamelToPhrase();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindowControl"/> class.
        /// </summary>
        public SettingsWindowControl()
        {
            this.InitializeComponent();
            this.Loaded += SettingsWindowControl_Loaded;
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
                    Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description
                })
                .ForEach(this.Settings.Add);

            this.featureSelector.SelectedItem = Settings.FirstOrDefault();
        }

        Window ParentWindow => this.FindParent<Window>();

        private void SettingsWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            var hostWindow = this.ParentWindow;
            if (hostWindow != null)
            {
                // there is no way to adjust window size from the designer
                hostWindow.Width = 600;
                hostWindow.Height = 420;
                hostWindow.Closed += HostWindow_Closed;
            }

            dirSelector.SelectedItem = dirSelector.Items[0];
            RefreshStatus();
            RefreshSettings();

            DataContext = this;
        }

        private void HostWindow_Closed(object sender, EventArgs e)
        {
            Runtime.Settings.Save();
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

                            var formattedCode = root.AlignIndents().ToFullString();

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

            set
            {
                description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

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
            Description = featureSelector.SelectedItem
                                         .CastTo<SettingsItem>()
                                         .Description;
        }
    }
}