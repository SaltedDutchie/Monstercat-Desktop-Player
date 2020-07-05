using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using CefSharp;
using CefSharp.WinForms;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Monstecat_Desktop_Player
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser browser;
        public DiscordRpcClient discord;

        public string currentSong = "";
        public string currentSongTime = "0";
        public DateTime songStart = DateTime.UtcNow;

        public void InitBrowser()
        {
            CefSettings settings = new CefSettings();
            settings.CachePath = Environment.CurrentDirectory + @"\CEF";

            Cef.Initialize(settings);
            browser = new ChromiumWebBrowser("https://www.monstercat.com/catalog?types=Single%2CEP%2CAlbum");
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
        }

        public void InitDiscord()
        {
            discord = new DiscordRpcClient("679284045141770250");

            discord.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            discord.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            discord.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };

            discord.Initialize();
        }

        public Form1()
        {
            InitializeComponent();
            InitBrowser();
            InitDiscord();

            Thread songThread = new Thread(async () =>
            {
                while (true)
                {
                    if (browser.IsBrowserInitialized)
                    {
                        var songTitle = await browser.GetMainFrame()
                            .EvaluateScriptAsync("(function() { return document.getElementsByClassName('scroll-title')[0].innerText; })();", null);
                        var songTime = await browser.GetMainFrame()
                            .EvaluateScriptAsync("(function() { return player.audio.currentTime; })();");

                        try
                        {
                            if (songTime.Result.ToString() != currentSongTime)
                            {
                                if (currentSong != songTitle.Result.ToString())
                                {
                                    currentSong = songTitle.Result.ToString();
                                    songStart = DateTime.UtcNow;
                                }

                                currentSongTime = songTime.Result.ToString();
                            }
                            else
                            {
                                currentSong = "";
                                songStart = DateTime.UtcNow;
                            }
                        }
                        catch
                        {
                            currentSong = "";
                            songStart = DateTime.UtcNow;
                            Console.WriteLine("Error grabbing song text. Probably hasn't loaded yet.");
                        }
                        
                        using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "SongText.txt")))
                        {
                            outputFile.Write(currentSong);
                        }
                    }

                    Thread.Sleep(2500);
                }
            });

            Thread discordThread = new Thread(async () =>
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
                                Timestamps = new Timestamps()
                                {
                                    Start = songStart
                                },
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

            songThread.IsBackground = true;
            songThread.Start();
            discordThread.IsBackground = true;
            discordThread.Start();
        }
    }
}
