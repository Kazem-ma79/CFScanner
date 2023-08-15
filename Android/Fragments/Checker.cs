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
using Core;
using Google.Android.Material.Dialog;
using System.Runtime.Remoting.Contexts;

namespace CFScanner.Fragments
{
    public class Checker : Fragment
    {
        private TextInputEditText hostnameInput;
        private TextInputEditText pathInput;
        private TextInputEditText portInput;
        private TextInputEditText CFIPInput;
        private Button checkButton;
        private TextView resultTextView;
        private string errorInternet, errorInputNull, stateGood, stateBad, stateError;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.page_checker, container, false);

            hostnameInput = view.FindViewById<TextInputEditText>(Resource.Id.hostnameInput);
            pathInput = view.FindViewById<TextInputEditText>(Resource.Id.pathInput);
            portInput = view.FindViewById<TextInputEditText>(Resource.Id.portInput);
            CFIPInput = view.FindViewById<TextInputEditText>(Resource.Id.CFIPInput);
            checkButton = view.FindViewById<Button>(Resource.Id.checkButton);
            resultTextView = view.FindViewById<TextView>(Resource.Id.textResult);

            checkButton.Click += CheckButton_Click;

            errorInternet = Activity.GetStringFromResources(Resource.String.error_no_internet);
            errorInputNull = Activity.GetStringFromResources(Resource.String.error_incomplete_input);
            stateGood = Activity.GetStringFromResources(Resource.String.state_good);
            stateBad = Activity.GetStringFromResources(Resource.String.state_bad);
            stateError = Activity.GetStringFromResources(Resource.String.state_error);

            return view;
        }
        private async void CheckButton_Click(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess != Xamarin.Essentials.NetworkAccess.Internet)
            {
                Toast.MakeText(Activity, errorInternet, ToastLength.Short).Show();
                return;
            }
            if (string.IsNullOrEmpty(hostnameInput.Text) || string.IsNullOrEmpty(portInput.Text) || string.IsNullOrEmpty(CFIPInput.Text) || string.IsNullOrEmpty(pathInput.Text))
            {
                Toast.MakeText(Activity, errorInputNull, ToastLength.Long).Show();
                return;
            }
            var madb = new AndroidX.AppCompat.App.AlertDialog.Builder(Activity)
                .SetTitle("Checking")
                .SetMessage("Checking domain status on IP ....")
                .SetIcon(Resource.Drawable.triad_ring)
                .SetCancelable(true)
                .Show();

            string hostname = hostnameInput.Text;
            string path = pathInput.Text;
            string cfip = CFIPInput.Text;
            int port = int.Parse(portInput.Text);

            var result = await CFDScanner.Scan(hostname, port, cfip, path);
            SetResult(IsGood: result.Status == ScanResult.ScanStatus.Success,
                Error: result.Status != ScanResult.ScanStatus.Success && result.Status != ScanResult.ScanStatus.Filtered);

            madb.Cancel();
        }

        private void SetResult(bool IsGood, bool Error = false)
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
            {
                if (!Error)
                    Activity.RunOnUiThread(() =>
                    {
                        resultTextView.SetTextColor(dangerColor);

                        resultTextView.SetText(stateBad, TextView.BufferType.Normal);
                    });
                else
                    Activity.RunOnUiThread(() =>
                    {
                        resultTextView.SetTextColor(dangerColor);

                        resultTextView.SetText(stateError, TextView.BufferType.Normal);
                    });
            }
        }
    }
}