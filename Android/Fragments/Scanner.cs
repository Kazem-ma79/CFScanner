using AndroidX.Fragment.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Google.Android.Material.BottomNavigation;
using System.Threading;
using Android.Content.Res;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Google.Android.Material.TextField;
using AndroidX.Core.Content;
using Xamarin.Essentials;
using Google.Android.Material.SwitchMaterial;
using Google.Android.Material.MaterialSwitch;
using AndroidX.AppCompat.Widget;
using Core;

namespace CFScanner.Fragments
{
    public class Scanner : Fragment
    {
        private TextInputEditText hostnameInput;
        private TextInputEditText pathInput;
        private TextInputEditText portInput;
        private TextInputEditText threadCountInput;
        private Button scanButton;
        private ProgressBar progressBar;
        private TextView progressText;
        private ListView goodIpListView;
        private List<string> goodIps, scannedIps;
        private ArrayAdapter<string> goodIpAdapter;
        private string errorInternet, errorInputNull, scanLabel, cancelLabel, clipboardMessage;

        List<Thread> threads;
        List<string> cdnIps;
        string hostname, path;
        int port, thread, IPiterator = 0;

        private CancellationTokenSource cancellationTokenSource;
        private AssetManager assetManager;
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.page_scanner, container, false);

            hostnameInput = view.FindViewById<TextInputEditText>(Resource.Id.hostnameInput);
            pathInput = view.FindViewById<TextInputEditText>(Resource.Id.pathInput);
            portInput = view.FindViewById<TextInputEditText>(Resource.Id.portInput);
            threadCountInput = view.FindViewById<TextInputEditText>(Resource.Id.threadCountInput);
            scanButton = view.FindViewById<Button>(Resource.Id.checkButton);
            progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
            progressText = view.FindViewById<TextView>(Resource.Id.textProgress);
            goodIpListView = view.FindViewById<ListView>(Resource.Id.goodIpListView);

            errorInternet = Activity.GetStringFromResources(Resource.String.error_no_internet);
            errorInputNull = Activity.GetStringFromResources(Resource.String.error_incomplete_input);
            scanLabel = Activity.GetStringFromResources(Resource.String.button_scan);
            cancelLabel = Activity.GetStringFromResources(Resource.String.cancel);
            clipboardMessage = Activity.GetStringFromResources(Resource.String.clipboard_set);

            goodIps = new List<string>();
            scannedIps = new List<string>();

            goodIpAdapter = new ArrayAdapter<string>(Activity, Android.Resource.Layout.SimpleListItem1, goodIps);

            goodIpListView.Adapter = goodIpAdapter;

            scanButton.Click += ScanButton_Click;
            goodIpListView.ItemClick += GoodIpListView_ItemClick;

            assetManager = Activity.Assets;
            cdnIps = assetManager.GetCDNIPList();
            threads = new List<Thread>();
            hostname = string.Empty;
            path = "/";

            return view;
        }

        private void GoodIpListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Activity.RunOnUiThread(async () =>
            {
                var pos = e.Position;
                var item = goodIpListView.GetItemAtPosition(pos).ToString();
                string selectedIP = item.Split(':')[0];
                await Clipboard.SetTextAsync(selectedIP);
                Toast.MakeText(Activity, $"{selectedIP} {clipboardMessage}", ToastLength.Short).Show();
            });
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess != Xamarin.Essentials.NetworkAccess.Internet)
            {
                Toast.MakeText(Activity, errorInternet, ToastLength.Short).Show();
                return;
            }
            if (string.IsNullOrEmpty(hostnameInput.Text) || string.IsNullOrEmpty(portInput.Text) || string.IsNullOrEmpty(threadCountInput.Text))
            {
                Toast.MakeText(Activity, errorInputNull, ToastLength.Long).Show();
                return;
            }

            if (scanButton.Text == scanLabel)
            {
                UpdateScanButtonText(cancelLabel);
            }
            else
            {
                UpdateScanButtonText(scanLabel);
            }

            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                // Cancel the operation
                cancellationTokenSource.Cancel();
            }
            else
            {
                // Clear previous results
                progressBar.Progress = 0;
                progressBar.Max = cdnIps.Count;
                progressText.Text = "0%";
                scannedIps.Clear();
                goodIpAdapter.Clear();
                goodIpAdapter.NotifyDataSetChanged();
                threads.Clear();
                cancellationTokenSource = new CancellationTokenSource();
                hostname = hostnameInput.Text;
                path = pathInput.Text;
                port = Convert.ToInt32(portInput.Text);
                thread = Convert.ToInt32(threadCountInput.Text);
                IPiterator = 0;
                progressText.Text = "0%";

                cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    for (int j = 0; j < thread; j++)
                    {
                        threads.Add(new Thread(Scan)
                        {
                            IsBackground = true,
                            Name = $"scanner #{j}"
                        });
                        threads[j].Start();
                    }
                }
                catch (System.OperationCanceledException ex)
                {
                    // Handle cancellation
                    Activity.RunOnUiThread(() =>
                    {
                        UpdateScanButtonText(scanLabel);
                    });
                }
                catch (Exception ex)
                {
                    Toast.MakeText(Activity, ex.Message, ToastLength.Long).Show();
                }
                finally
                {
                    Activity.RunOnUiThread(() =>
                    {
                        UpdateScanButtonText(scanLabel);
                    });
                }

                UpdateScanButtonText(cancelLabel);
            }
        }

        private async void Scan()
        {
            while (IPiterator < cdnIps.Count)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    UpdateScanButtonText(scanLabel);
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
                    AddToGoodIpList($"{ip}\t:\t{result.Delay}ms");
                }
                SetProgressBar(IPiterator);
            }
            if (IPiterator == cdnIps.Count)
            {
                UpdateScanButtonText(scanLabel);
                cancellationTokenSource = new CancellationTokenSource();
            }
        }

        private void AddToGoodIpList(string ip)
        {
            Activity.RunOnUiThread(delegate
            {
                goodIpAdapter.Add(ip);
                goodIpAdapter.NotifyDataSetChanged();
            });
        }

        private void SetProgressBar(int count)
        {
            Activity.RunOnUiThread(() =>
            {
                progressBar.SetProgress(count, true);
                progressText.SetText($"{count} of {progressBar.Max}", TextView.BufferType.Normal);
            });
        }

        private void UpdateScanButtonText(string buttonText)
        {
            Activity.RunOnUiThread(() =>
            {
                scanButton.Text = buttonText;
            });
        }
    }
}
