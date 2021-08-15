using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Threading;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;

//using Newtonsoft.Json;

namespace MindustryInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (!File.Exists("Newtonsoft.Json.dll"))
            {
                MessageBox.Show("Necessery dll not found. This program will download it.\nPlese wait.");
                using (var wc = new WebClient())
                {
                    wc.DownloadFile("https://github.com/dayo05/MindustryInstaller/releases/download/1.0/Newtonsoft.Json.dll", "Newtonsoft.Json.dll");
                }
            }
            InitializeComponent();

            new Thread(LoadVersionOfLauncher).Start();
            Directory.CreateDirectory(InstallDir);
            Directory.CreateDirectory(TempDir);
        }
        bool DownloadJAVA = true;
        string InstallDir = @"C:\Program Files\Mindustry\";
        string TempDir = Path.GetTempPath() + "MindustryInstaller";
        bool DownloadStable = true;
        string version = null;
        string launcherVersion = null;
        int downloadstat = 0;
        int tick = 0;
        object sync;
        void PrintLog(string log)
        {
            Invoke(() => LogTextBlock.Text += log + '\n');
            Invoke(() => logView.ScrollToBottom());
        }
        //Gets version of launcher
        void LoadVersionOfLauncher()
        {
            Invoke(() => InstallButton.IsEnabled = false);
            Invoke(() => UseStableVersionCheckbox.IsEnabled = false);
            using (var wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "ua";
                PrintLog("Searching launcher version");
                launcherVersion = wc.DownloadString("https://api.github.com/repos/Dayo05/MindustryInstaller/releases/latest");
                launcherVersion = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(launcherVersion)["tag_name"] as string;
                PrintLog($"Launcher version: {launcherVersion} found");
            }
        }
        //Gets version of Mindustry
        void LoadVersion()
        {
            while (launcherVersion == null) { }
            Invoke(() => InstallButton.IsEnabled = false);
            Invoke(() => UseStableVersionCheckbox.IsEnabled = false);
            using (var wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "ua";
                if (DownloadStable)
                {
                    PrintLog("Searching stable version");
                    version = wc.DownloadString("https://api.github.com/repos/Anuken/Mindustry/releases/latest");
                    version = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(version)["tag_name"] as string;
                }
                else
                {
                    PrintLog("Searching preview version");
                    version = wc.DownloadString("https://api.github.com/repos/Anuken/Mindustry/releases");
                    version = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(version)[0]["tag_name"] as string;
                }
                PrintLog($"Mindustry version: {version} found");
                Invoke(() => VersionLabel.Content = "Mindustry version: " + version);
            }
            Invoke(() => InstallButton.IsEnabled = true);
            Invoke(() => UseStableVersionCheckbox.IsEnabled = true);
        }
        void Invoke(Action a)
            => Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate() { a(); });
        //Install Mindustry
        void Install()
        {
            Invoke(() => InstallButton.IsEnabled = false);
            Invoke(() => DownloadJAVA = DownloadJavaCheckbox.IsChecked.Value);
            PrintLog("Download Java: " + (DownloadJAVA ? "true" : "false"));
            PrintLog("Download version: " + version);
            try
            {
                Directory.CreateDirectory(InstallDir);
                using (var wc = new WebClient())
                {
                    wc.DownloadProgressChanged += WC_DownloadProgressChanged;
                    wc.DownloadFileCompleted += WC_DownloadFileCompleted;
                    wc.Headers["User-Agent"] = "MindustryInstaller";
                    PrintLog("Setup: WebClient");
                    if (DownloadJAVA)
                    {
                        try
                        {
                            Directory.Delete(TempDir + @"java", true);
                        }
                        catch (DirectoryNotFoundException) { }
                        try
                        {
                            Directory.Delete(InstallDir + @"java", true);
                        }
                        catch (DirectoryNotFoundException) { }
                        downloadstat = 1;
                        sync = new object();
                        lock (sync)
                        {
                            wc.DownloadFileAsync(new Uri("https://download.java.net/java/GA/jdk16.0.2/d4a915d82b4c4fbb9bde534da945d746/7/GPL/openjdk-16.0.2_windows-x64_bin.zip"), TempDir + "mindustry-java.zip");
                            Monitor.Wait(sync);
                        }
                        PrintLog("Extrecting JAVA Java to " + InstallDir + "java");
                        ZipFile.ExtractToDirectory(TempDir + "mindustry-java.zip", TempDir + "java");
                        Directory.Move(TempDir + @"java\jdk-16.0.2", InstallDir + "java");
                    }
                    if (File.Exists(InstallDir + "Mindustry.jar"))
                        File.Delete(InstallDir + "Mindustry.jar");
                    if (File.Exists(InstallDir + "Mindustry.exe"))
                        File.Delete(InstallDir + "Mindustry.exe");
                    downloadstat = 2;
                    sync = new object();
                    lock (sync)
                    {
                        PrintLog("Downloading Mindustry");
                        wc.DownloadFileAsync(new Uri($"https://github.com/Anuken/Mindustry/releases/download/{version}/Mindustry.jar"), InstallDir + "Mindustry.jar");
                        Monitor.Wait(sync);
                    }
                    downloadstat = 3;
                    sync = new object();
                    lock (sync)
                    {
                        PrintLog("Downloading Launcher");
                        wc.DownloadFileAsync(new Uri($"https://github.com/dayo05/MindustryInstaller/releases/download/{launcherVersion}/MindustryLauncher.exe"), InstallDir + "Mindustry.exe");
                        Monitor.Wait(sync);
                    }
                }
                PrintLog("Cleaning files");
                try
                {
                    Directory.Delete(TempDir + @"java", true);
                }
                catch (DirectoryNotFoundException) { }
                try
                {
                    File.Delete(TempDir + "mindustry-java.zip");
                }
                catch (DirectoryNotFoundException) { }
            }
            catch (Exception e)
            {
                _ = MessageBox.Show(e.Message);
            }
            downloadstat = 0;
            PrintLog("Finished.");
            Invoke(() => InstallButton.IsEnabled = true);
        }

        private void WC_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            lock (sync)
            {
                if (downloadstat == 1)
                    PrintLog("Java Downloaded");
                else if (downloadstat == 2)
                    PrintLog("Mindustry Downloaded");
                else if (downloadstat == 3)
                    PrintLog("Launcher Downloaded");
                Monitor.Pulse(sync);
            }
        }

        private void WC_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (tick++ % 100 == 0)
            {
                if (downloadstat == 1)
                    PrintLog($"Downloading JAVA...{e.ProgressPercentage}% ({e.BytesReceived} / {e.TotalBytesToReceive})");
                else if (downloadstat == 2)
                    PrintLog($"Downloading Mindustry...{e.ProgressPercentage}% ({e.BytesReceived} / {e.TotalBytesToReceive})");
                else if (downloadstat == 3)
                    PrintLog($"Downloading Launcher...{e.ProgressPercentage}% ({e.BytesReceived} / {e.TotalBytesToReceive})");
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
            => new Thread(() => Install()).Start();

        private void UseStableVersionCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DownloadStable = true;
            new Thread(() => LoadVersion()).Start();
        }

        private void UseStableVersionCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DownloadStable = false;
            new Thread(() => LoadVersion()).Start();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0); // For kill all threads.
        }
    }
}
