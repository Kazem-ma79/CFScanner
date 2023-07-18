using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private CancellationTokenSource cancellationTokenSource;
        private ObservableCollection<string> Results;
        private BackgroundWorker Worker;

        public MainWindow()
        {
            InitializeComponent();
            CloudflareIP.Height = Port.Height;
            cancellationTokenSource = new CancellationTokenSource();
            Worker = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true
            };
            Results = new();
            Result.ItemsSource = Results;
            Result.SelectionChanged += Result_SelectionChanged;
            Worker.DoWork += Worker_Job;
        }

        private async void Result_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = Result.SelectedItem.ToString();
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

        private async void Worker_Job(object? sender, DoWorkEventArgs e)
        {
            try
            {
                var args = (ValueTuple<string[], string, string, int, int>)e.Argument;
                string[] cdnIps = args.Item1;
                string proto = args.Item2,
                    hostname = args.Item3;
                int port = args.Item4,
                    thread = args.Item5,
                    progress = 0;

                Dispatcher.Invoke(() =>
                {
                    ScanProgress.Value = 0;
                    ScanProgress.Maximum = cdnIps.Length;
                });

                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = thread,
                    CancellationToken = cancellationTokenSource.Token
                };

                await Parallel.ForEachAsync(cdnIps, options, async (ip, ct) =>
                {
                    if (ct.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StartBtn.IsEnabled = true;
                            CancelBtn.IsEnabled = false;
                        });
                        return;
                    }
                    var result = await CheckIp(proto, $"{ip}:{port}", hostname);
                    if (result)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Results.Add(ip);
                        });
                    }
                    Dispatcher.Invoke(() =>
                    {
                        ScanProgress.Value = ++progress;
                        ProgressLabel.Content = $"{progress} of {cdnIps.Length}";
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Dispatcher.Invoke(() =>
            {
                StartBtn.IsEnabled = true;
                CancelBtn.IsEnabled = false;
            });
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            string[] cdnIps = Properties.Resources.cdn.Split(Environment.NewLine);
            string proto = SSL.IsOn ? "https" : "http",
                hostname = Hostname.Text,
                cfIP = CloudflareIP.Text;
            int port = Convert.ToInt32(Port.Value),
                thread = Convert.ToInt32(Thread.Value);

            if (Mode.IsOn) // Scanner Mode
            {
                Worker.RunWorkerAsync(argument: (cdnIps, proto, hostname, port, thread));
                StartBtn.IsEnabled = false;
                CancelBtn.IsEnabled = true;
            }
            else // Checker Mode
            {
                var result = await CheckIp(proto, $"{cfIP}:{port}", hostname);
                var mySettings = new MetroDialogSettings()
                {
                    AnimateShow = true,
                    AnimateHide = true
                };
                string errorMessage = result ? "IP is good" : "IP is filtered";
                await this.ShowMessageAsync("Checking Done!",
                    errorMessage,
                    MessageDialogStyle.Affirmative, mySettings);
            }
        }

        private async Task<bool> CheckIp(string proto, string ip, string hostname)
        {
            string url = $"{proto}://{ip}/";

            HttpClientHandler httpClientHandler = new()
            {
                AllowAutoRedirect = false
            };

            using HttpClient client = new(httpClientHandler)
            {
                BaseAddress = new Uri(url)
            };
            client.DefaultRequestHeaders.Host = hostname;

            try
            {
                var result = await client.GetAsync(url);
                var statusCode = result.StatusCode;
                string resultContent = await result.Content.ReadAsStringAsync();
                return (statusCode == HttpStatusCode.BadRequest && !resultContent.ToLower().Contains("plain http request was sent to https port"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!Mode.IsOn)
                        MessageBox.Show(ex.Message);
                });
                return false;
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
    }
}
