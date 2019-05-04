using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CMPlus;

namespace Testpad
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            new Application().Run(new HostWindow());
        }
    }

    class HostWindow : Window
    {
        public HostWindow()
        {
            this.Content = new SettingsWindowControl();
        }
    }
}