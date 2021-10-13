using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using CefSharp;
using CefSharp.Wpf;
using DiscordRPC;

namespace Monstercat_Desktop_Player
{
    public partial class MainWindow : Window
    {
        public ChromiumWebBrowser browser;
        public DiscordRpcClient discord;

        public string currentSong = "Init";

        public void InitBrowser()
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory + @"\CEF";

            Cef.Initialize(settings);
            browser = new ChromiumWebBrowser("https://www.monstercat.com/player");
            Grid.SetRow(browser, 0);
            MainGrid.Children.Add(browser);
        }

        public void InitDiscord()
        {
            discord = new DiscordRpcClient("679284045141770250");
            discord.Initialize();
        }

        public MainWindow()
        {
            InitializeComponent();
            InitBrowser();
            InitDiscord();

            Thread songThread = new Thread(() =>
            {
                while (true)
                {
                    if (Dispatcher.Invoke(() => { return browser.IsBrowserInitialized; }))
                    {
                        var songTitle = Dispatcher.Invoke(async () => { return await browser.GetMainFrame().EvaluateScriptAsync("(function () { return document.getElementsByClassName('cursor-pointer release-link')[0].innerText; })();", null); });
                        
                        try
                        {
                            if (currentSong != songTitle.Result.Result.ToString())
                            {
                                currentSong = songTitle.Result.Result.ToString();
                                using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "SongText.txt")))
                                {
                                    outputFile.Write(currentSong);
                                }
                            }
                        }
                        catch
                        {
                            currentSong = "";
                            Console.WriteLine("Error fetching song information.");
                        }
                    }

                    Thread.Sleep(2500);
                }
            });

            Thread discordThread = new Thread(() =>
            {
                while (true)
                {
                    if (discord.IsInitialized)
                    {
                        if (currentSong != "")
                        {
                            discord.SetPresence(new RichPresence()
                            {
                                Details = "Now Playing",
                                State = currentSong,
                                Assets = new Assets()
                                {
                                    LargeImageKey = "monstercat_logo_trans",
                                    LargeImageText = "Monstercat"
                                }
                            });
                        }
                        else
                        {
                            discord.SetPresence(new RichPresence()
                            {
                                Details = "Nothing Playing",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "monstercat_logo_trans",
                                    LargeImageText = "Monstercat"
                                }
                            });
                        }
                    }

                    Thread.Sleep(15000);
                }
            });

            songThread.Start();
            discordThread.Start();
        }
    }
}
