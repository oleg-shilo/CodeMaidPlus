using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CMPlus
{
    /// <summary>
    /// Interaction logic for SettingsWindowControl.
    /// </summary>
    public partial class SettingsWindowControl : UserControl
    {
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
                .Select(p => new SettingsItem { Enabled = (bool)p.GetValue(Runtime.Settings), Name = p.Name })
                .ForEach(this.Settings.Add);
        }

        Window ParentWindow => this.FindParent<Window>();

        private void SettingsWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            var hostWindow = this.ParentWindow;
            if (hostWindow != null)
            {
                // there is no way to adjust window size from the designer
                hostWindow.Width = 410;
                hostWindow.Height = 225;
                hostWindow.Closed += HostWindow_Closed;
            }

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
        }

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
    }
}