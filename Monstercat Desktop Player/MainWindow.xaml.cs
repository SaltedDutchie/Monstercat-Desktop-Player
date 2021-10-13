using System;
using System.Windows;
using System.Windows.Controls;

using CefSharp;
using CefSharp.Wpf;

namespace Monstercat_Desktop_Player
{
    public partial class MainWindow : Window
    {
        public ChromiumWebBrowser browser;

        public void InitBrowser()
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory + @"\CEF";

            Cef.Initialize(settings);
            browser = new ChromiumWebBrowser("https://www.monstercat.com/player");
            Grid.SetRow(browser, 0);
            MainGrid.Children.Add(browser);
        }

        public MainWindow()
        {
            InitializeComponent();
            InitBrowser();
        }
    }
}
