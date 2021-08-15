using System;
using System.IO;
using System.Net;
using System.Windows;
using System.IO.Compression;
using System.Collections.Generic;

using Microsoft.Win32;

using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MindustryInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(InstallDir);
            Directory.CreateDirectory(TempDir);
        }
        bool DownloadJAVA = true;
        string InstallDir = @"C:\Program Files\Mindustry\";
        string TempDir = Path.GetTempPath() + "MindustryInstaller";
        bool DownloadStable = true;
        string version = null;
        int downloadstat = 0;
        int tick = 0;
        void PrintLog(string log)
        {
            Invoke(() => LogTextBlock.Text += log + '\n');
            Invoke(() => logView.ScrollToBottom());
        }
        
        //Gets version of Mindustry
        void LoadVersion()
        {
            Invoke(() => InstallButton.IsEnabled = false);
            Invoke(() => UseStableVersionCheckbox.IsEnabled = false);
            using (var wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "ua";
                if (DownloadStable)
                {
                    PrintLog("Searching stable version");
                    version = wc.DownloadString("https://api.github.com/repos/Anuken/Mindustry/releases/latest");
                    version = JsonConvert.DeserializeObject<Dictionary<string, object>>(version)["tag_name"] as string;
                }
                else
                {
                    PrintLog("Searching preview version");
                    version = wc.DownloadString("https://api.github.com/repos/Anuken/Mindustry/releases");
                    version = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(version)[0]["tag_name"] as string;
                }
                PrintLog($"{version} found");
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
                            Directory.Delete(Path.GetTempPath() + @"java", true);
                        }
                        catch (DirectoryNotFoundException) { }
                        downloadstat = 1;
                        var sync = new object();
                        lock (sync)
                        {
                            wc.DownloadFileAsync(new Uri("https://download.java.net/java/GA/jdk16.0.2/d4a915d82b4c4fbb9bde534da945d746/7/GPL/openjdk-16.0.2_windows-x64_bin.zip"), Path.GetTempPath() + "mindustry-java.zip");
                            Monitor.Wait(sync);
                        }
                        PrintLog("Extrecting JAVA Java to " + InstallDir + "java");
                        ZipFile.ExtractToDirectory(Path.GetTempPath() + "mindustry-java.zip", Path.GetTempPath() + "java");
                        Directory.Move(Path.GetTempPath() + @"java\jdk-16.0.2", InstallDir + "java");
                    }
                    if (File.Exists(InstallDir + "Mindustry.jar"))
                        File.Delete(InstallDir + "Mindustry.jar");
                    downloadstat = 2;
                    var sync2 = new object();
                    lock (sync2)
                    {
                        wc.DownloadFile($"https://github.com/Anuken/Mindustry/releases/download/{version}/Mindustry.jar", InstallDir + "Mindustry.jar");
                        Monitor.Wait(sync2);
                    }
                }
                PrintLog("Cleaning files");
                try
                {
                    Directory.Delete(Path.GetTempPath() + @"java", true);
                }
                catch (DirectoryNotFoundException) { }
                try
                {
                    File.Delete(Path.GetTempPath() + "mindustry-java.zip");
                }
                catch (DirectoryNotFoundException) { }
            }
            catch (Exception e)
            {
                _ = MessageBox.Show(e.Message);
            }
            downloadstat = 0;
            Invoke(() => InstallButton.IsEnabled = true);
        }

        private void WC_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (downloadstat == 1)
                PrintLog("Java Downloaded");
            else if (downloadstat == 2)
                PrintLog("Mindustry Downloaded");
            Monitor.Pulse(e.UserState);
        }

        private void WC_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (tick++ % 100 == 0)
            {
                if (downloadstat == 1)
                    PrintLog($"Downloading JAVA...{e.ProgressPercentage}% ({e.BytesReceived} / {e.TotalBytesToReceive})");
                else if (downloadstat == 2)
                    PrintLog($"Downloading Mindustry...{e.ProgressPercentage}% ({e.BytesReceived} / {e.TotalBytesToReceive})");
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
    }
}
