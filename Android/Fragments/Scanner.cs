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

namespace CFScanner.Fragments
{
    public class Scanner : Fragment
    {
        private TextInputEditText hostnameInput;
        private TextInputEditText portInput;
        private TextInputEditText threadCountInput;
        private SwitchCompat sslInput;
        private Button scanButton;
        private ProgressBar progressBar;
        private TextView progressText;
        private ListView goodIpListView;
        private List<string> goodIps, scannedIps;
        private ArrayAdapter<string> goodIpAdapter;
        private string errorInternet, errorInputNull, scanLabel, cancelLabel, clipboardMessage;

        private CancellationTokenSource cancellationTokenSource;
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.page_scanner, container, false);

            hostnameInput = view.FindViewById<TextInputEditText>(Resource.Id.hostnameInput);
            portInput = view.FindViewById<TextInputEditText>(Resource.Id.portInput);
            threadCountInput = view.FindViewById<TextInputEditText>(Resource.Id.threadCountInput);
            sslInput = view.FindViewById<SwitchCompat>(Resource.Id.sslInput);
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
                progressText.Text = "0%";
                scannedIps.Clear();
                goodIpAdapter.Clear();
                goodIpAdapter.NotifyDataSetChanged();

                cancellationTokenSource = new CancellationTokenSource();

                Task.Run(() =>
                {
                    string proto = sslInput.Checked ? "https" : "http";
                    string hostname = hostnameInput.Text;
                    int port = int.Parse(portInput.Text);
                    int threadCount = int.Parse(threadCountInput.Text);

                    try
                    {
                        StartIpScan(proto, hostname, port, threadCount, cancellationTokenSource.Token);
                    }
                    catch (System.OperationCanceledException ex)
                    {
                        Console.WriteLine(ex.Message);
                        // Handle cancellation
                        Activity.RunOnUiThread(() =>
                        {
                            UpdateScanButtonText(scanLabel);
                        });
                    }
                    finally
                    {
                        Activity.RunOnUiThread(() =>
                        {
                            UpdateScanButtonText(scanLabel);
                        });
                    }
                });

                UpdateScanButtonText(cancelLabel);
            }
        }

        private void StartIpScan(string proto, string hostname, int port, int threadCount, CancellationToken cancellationToken)
        {
            try
            {
                AssetManager assetManager = Activity.Assets;
                string filename = "cdn.txt";

                using StreamReader sr = new StreamReader(assetManager.Open(filename));
                string fileContents = sr.ReadToEnd();
                string[] cdnIps = fileContents.Split("\r\n");
                progressBar.Max = cdnIps.Length;
                var p = Parallel.ForEach(cdnIps, new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = cancellationToken }, ip =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    CheckIp(proto, $"{ip}:{port}", hostname);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private void CheckIp(string proto, string ip, string hostname)
        {
            string url = $"{proto}://{ip}/";

            try
            {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(url);
                Request.Method = "GET";
                Request.KeepAlive = true;
                Request.AllowAutoRedirect = false;
                Request.Host = hostname;

                HttpWebResponse response = (HttpWebResponse)Request.GetResponse();

                HttpStatusCode statusCode = response.StatusCode;

                if (statusCode == HttpStatusCode.BadRequest)
                {
                    AddToGoodIpList(ip);
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var wex = ex as WebException;
                    if (wex.Response is HttpWebResponse response)
                    {
                        Stream responseStream = response.GetResponseStream();

                        StreamReader streamReader = new StreamReader(responseStream, Encoding.Default);

                        string responseContent = streamReader.ReadToEnd();
                        HttpStatusCode statusCode = response.StatusCode;

                        if (statusCode == HttpStatusCode.BadRequest)
                        {
                            AddToGoodIpList(ip);
                        }
                        else if (statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Found)
                        {
                            Console.WriteLine("Filtered: " + ip);
                        }

                        streamReader.Close();
                        responseStream.Close();
                        response.Close();
                    }
                    else
                    {
                        Console.WriteLine($"{ip} => {ex.Message}");
                    }
                }
                else
                    Toast.MakeText(Activity, ex.Message, ToastLength.Short);
            }
            scannedIps.Add(ip);
            SetProgressBar(scannedIps.Count);
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
