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
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net;
using System.IO;
using Xamarin.Essentials;
using Google.Android.Material.TextField;
using AndroidX.Core.Content;
using Google.Android.Material.MaterialSwitch;
using AndroidX.AppCompat.Widget;

namespace CFScanner.Fragments
{
    public class Checker : Fragment
    {
        private TextInputEditText hostnameInput;
        private TextInputEditText portInput;
        private TextInputEditText CFIPInput;
        private SwitchCompat sslInput;
        private Button checkButton;
        private TextView resultTextView;
        private string errorInternet, errorInputNull, stateGood, stateBad;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.page_checker, container, false);

            hostnameInput = view.FindViewById<TextInputEditText>(Resource.Id.hostnameInput);
            portInput = view.FindViewById<TextInputEditText>(Resource.Id.portInput);
            CFIPInput = view.FindViewById<TextInputEditText>(Resource.Id.CFIPInput);
            sslInput = view.FindViewById<SwitchCompat>(Resource.Id.sslInput);
            checkButton = view.FindViewById<Button>(Resource.Id.checkButton);
            resultTextView = view.FindViewById<TextView>(Resource.Id.textResult);

            checkButton.Click += CheckButton_Click;

            errorInternet = Activity.GetStringFromResources(Resource.String.error_no_internet);
            errorInputNull = Activity.GetStringFromResources(Resource.String.error_incomplete_input);
            stateGood = Activity.GetStringFromResources(Resource.String.state_good);
            stateBad = Activity.GetStringFromResources(Resource.String.state_bad);

            return view;
        }
        private async void CheckButton_Click(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess != Xamarin.Essentials.NetworkAccess.Internet)
            {
                Toast.MakeText(Activity, errorInternet, ToastLength.Short).Show();
                return;
            }
            if (string.IsNullOrEmpty(hostnameInput.Text) || string.IsNullOrEmpty(portInput.Text) || string.IsNullOrEmpty(CFIPInput.Text))
            {
                Toast.MakeText(Activity, errorInputNull, ToastLength.Long).Show();
                return;
            }

            HttpStatusCode statusCode = 0;
            try
            {
                string proto = sslInput.Checked ? "https" : "http";
                string hostname = hostnameInput.Text;
                string cfip = CFIPInput.Text;
                int port = int.Parse(portInput.Text);
                string url = $"{proto}://{cfip}:{port}/";

                HttpClientHandler httpClientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false
                };

                using HttpClient client = new HttpClient(httpClientHandler)
                {
                    BaseAddress = new Uri(url)
                };
                client.DefaultRequestHeaders.Host = hostname;
                var result = await client.GetAsync(url);
                statusCode = result.StatusCode;
                string resultContent = await result.Content.ReadAsStringAsync();
            }
            catch(Exception ex)
            {
                if (ex is WebException)
                {
                    var wex = ex as WebException;
                    if (wex.Response is HttpWebResponse response)
                    {
                        Stream responseStream = response.GetResponseStream();

                        StreamReader streamReader = new StreamReader(responseStream, Encoding.Default);

                        string responseContent = streamReader.ReadToEnd();
                        statusCode = response.StatusCode;

                        streamReader.Close();
                        responseStream.Close();
                        response.Close();
                    }
                    else
                    {
                        Console.WriteLine("[-] " + ex.Message);
                    }
                }
                else
                    Toast.MakeText(Activity, ex.Message, ToastLength.Short);
            }

            if (statusCode == HttpStatusCode.Found || statusCode == HttpStatusCode.MovedPermanently)
                SetResult(false);
            else if (statusCode != 0)
                SetResult(true);
        }

        private void SetResult(bool IsGood)
        {
            int successColorId = Resource.Color.colorSuccess;
            int dangerColorId = Resource.Color.colorDanger;
            Android.Graphics.Color successColor = new Android.Graphics.Color(ContextCompat.GetColor(Activity, successColorId));
            Android.Graphics.Color dangerColor = new Android.Graphics.Color(ContextCompat.GetColor(Activity, dangerColorId));
            if (IsGood)
                Activity.RunOnUiThread(() =>
                {
                    resultTextView.SetTextColor(successColor);

                    resultTextView.SetText(stateGood, TextView.BufferType.Normal);
                });
            else
                Activity.RunOnUiThread(() =>
                {
                    resultTextView.SetTextColor(dangerColor);

                    resultTextView.SetText(stateBad, TextView.BufferType.Normal);
                });
        }
    }
}