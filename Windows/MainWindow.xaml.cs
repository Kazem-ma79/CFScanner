using Core;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;

        private CancellationTokenSource cancellationTokenSource;
        private readonly ObservableCollection<int> CFPorts = new(new int[] { 80, 443, 2052, 2053, 2082, 2083, 2086, 2087, 2095, 2096, 8080, 8880, 8443 });
        private ObservableCollection<ResultItem> Results;
        private CollectionViewSource ResultCollection;

        List<Thread> threads;
        List<string> cdnIps;
        string hostname, cfIP, path;
        int port, thread, IPiterator = 0;

        public MainWindow()
        {
            InitializeComponent();
            CloudflareIP.Height = Port.Height;

            hostname = cfIP = string.Empty;
            path = "/";

            cancellationTokenSource = new();
            threads = new();
            Results = new();
            ResultCollection = new()
            {
                Source = Results
            };
            ResultCollection.SortDescriptions.Add(new SortDescription("Ping", ListSortDirection.Ascending));
            Result.ItemsSource = ResultCollection.View;
            Result.SelectionChanged += Result_SelectionChanged;

            openFileDialog = new OpenFileDialog()
            {
                DefaultExt = ".txt",
                Filter = "Text Files|*.txt",
                Title = "Select IP list"
            };
            saveFileDialog = new SaveFileDialog()
            {
                DefaultExt = ".txt",
                Filter = "Text Files|*.txt",
                Title = "Save Good IP list"
            };
            openFileDialog.FileOk += LoadFile;
            saveFileDialog.FileOk += SaveFile;

            cdnIps = new(Properties.Resources.cdn.Split(Environment.NewLine));
        }

        private void AppMode_Changed(object sender, RoutedEventArgs e)
        {
            CloudflareIP.IsEnabled = !Mode.IsOn;
            Thread.IsEnabled = Mode.IsOn;
        }

        private async void Result_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Result.SelectedIndex >= 0)
            {
                string item = ((ResultItem)Result.SelectedItem).Ping.ToString();
                Clipboard.SetText(item);
                var mySettings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = true
                };
                await this.ShowMessageAsync("Copied!",
                    $@"{item} copied to clipboard.",
                    MessageDialogStyle.Affirmative, mySettings);
            }
        }

        private async void Scan()
        {
            while (IPiterator < cdnIps.Count)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StartBtn.IsEnabled = true;
                        CancelBtn.IsEnabled = false;
                    });
                    foreach (var item in threads)
                    {
                        item.Interrupt();
                        item.Join(500);
                    }
                    break;
                }
                var ip = cdnIps[IPiterator];
                IPiterator++;
                var result = await CFDScanner.Scan(hostname, port, ip, path, 3);
                if (result.Status == ScanResult.ScanStatus.Success)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Results.Add(new() { IP = ip, Ping = result.Delay });
                    });
                }
                Dispatcher.Invoke(() =>
                {
                    ScanProgress.Value = IPiterator;
                    ProgressLabel.Content = $"{IPiterator} of {cdnIps.Count}";
                });
            }
            if (IPiterator == cdnIps.Count)
            {
                Dispatcher.Invoke(() =>
                {
                    StartBtn.IsEnabled = true;
                    CancelBtn.IsEnabled = false;
                });
                cancellationTokenSource = new();
            }
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            #region Reset Values
            cancellationTokenSource = new();
            hostname = Hostname.Text;
            cfIP = CloudflareIP.Text;
            path = Path.Text;
            port = Convert.ToInt32(Port.Value);
            thread = Convert.ToInt32(Thread.Value);
            IPiterator = 0;
            ProgressLabel.Content = "";
            #endregion

            #region Input Validation
            bool hasError = false;
            string errorMsg = string.Empty;
            if (string.IsNullOrWhiteSpace(hostname))
            {
                hasError = true;
                errorMsg += $"Hostname{Environment.NewLine}";
            }
            if (port < 1 || port > 65534)
            {
                hasError = true;
                errorMsg += $"Port{Environment.NewLine}";
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                hasError = true;
                errorMsg += $"Path{Environment.NewLine}";
            }
            if (string.IsNullOrWhiteSpace(cfIP) && !Mode.IsOn)
            {
                hasError = true;
                errorMsg += $"Cloudflare IP{Environment.NewLine}";
            }
            if ((thread < 1 || thread > 1000000) && Mode.IsOn)
            {
                hasError = true;
                errorMsg += $"Thread Count{Environment.NewLine}";
            }
            if (hasError)
            {
                string errorMessage = $"Invalid fields: {Environment.NewLine}{errorMsg}";
                await this.ShowMessageAsync("Input error",
                    errorMessage,
                    MessageDialogStyle.Affirmative);
            }
            #endregion

            if (Mode.IsOn)
            {
                #region Scan Mode
                StartBtn.IsEnabled = false;
                CancelBtn.IsEnabled = true;
                ScanProgress.Maximum = cdnIps.Count;
                Results.Clear();

                threads.Clear();
                for (int j = 0; j < thread; j++)
                {
                    threads.Add(new(Scan)
                    {
                        IsBackground = true,
                        Name = $"scanner #{j}"
                    });
                    threads[j].Start();
                }
                #endregion
            }
            else
            {
                #region Check Mode
                var result = await CFDScanner.Scan(hostname, port, cfIP, path);
                var mySettings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = true
                };
                string errorMessage = $"Scan status: {result.Status}{Environment.NewLine}Delay: {result.Delay}";
                await this.ShowMessageAsync("Checking Done!",
                    errorMessage,
                    MessageDialogStyle.Affirmative, mySettings);
                #endregion
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Kazem-ma79/CFScanner/",
                UseShellExecute = true
            });
        }

        private void Import(object sender, RoutedEventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        private async void LoadFile(object? sender, CancelEventArgs e)
        {
            if (e.Cancel) return;
            var fileName = openFileDialog.FileName;
            var fileContent = await File.ReadAllLinesAsync(fileName);
            cdnIps = new(fileContent);

            if (cdnIps.Count == fileContent.Length)
            {
                var mySettings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = true
                };
                await this.ShowMessageAsync("Loading Done!",
                    $"Loaded {cdnIps.Count} IPs",
                    MessageDialogStyle.Affirmative, mySettings);
            }
        }

        private void Export(object sender, RoutedEventArgs e)
        {
            saveFileDialog.ShowDialog();
        }

        private async void SaveFile(object? sender, CancelEventArgs e)
        {
            if (e.Cancel) return;
            var tempItems = ResultCollection.View;
            var tempCount = Results.Count;
            var fileName = saveFileDialog.FileName;
            var jsonContent = JsonConvert.SerializeObject(tempItems, Formatting.Indented);
            await File.WriteAllTextAsync(fileName, jsonContent, Encoding.UTF8);

            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = true,
                AnimateHide = true
            };
            await this.ShowMessageAsync("Saving Done!",
                $"Saved {tempCount} IPs",
                MessageDialogStyle.Affirmative, mySettings);
        }
    }
}
