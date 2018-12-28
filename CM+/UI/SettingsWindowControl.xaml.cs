using System;
using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindowControl"/> class.
        /// </summary>
        public SettingsWindowControl()
        {
            this.InitializeComponent();
            this.Loaded += SettingsWindowControl_Loaded;
        }

        private void SettingsWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            var hostWindow = this.FindParent<Window>();
            if (hostWindow != null)
            {
                // there is no way to adjust window size from designer
                hostWindow.Width = this.Width;
                hostWindow.Height = this.Height + 25;
            }

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            integrate.IsEnabled = Settings.IsCmInstalled;

            if (Settings.IsIntegrated)
            {
                integrate.Content = "Unintegrate";
                status.Text = "Integrated";
            }
            else
            {
                integrate.Content = "Integrate";
                status.Text = "Unintegrated" + (Settings.IsCmInstalled ? "" : " (CodeMaid is not installed)");
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
            Settings.ToggleIntegration();
            RefreshStatus();

            var hostWindow = this.FindParent<Window>();
            if (hostWindow != null)
                hostWindow.Close();

            MessageBox.Show("The changes will take affect only when you restart Visual Studio or press 'Save' " +
                            "button in the CodeMaid 'Options' dialog.", "CM+ Settings");
        }

        private void help_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/oleg-shilo/CodeMaidPlus");
            }
            catch
            {
            }
        }
    }
}