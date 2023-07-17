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
using Xamarin.Essentials;

namespace CFScanner.Fragments
{
    public class About : Fragment
    {
        private LinearLayout github;
        private LinearLayout ircf;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.page_about, container, false);

            github = view.FindViewById<LinearLayout>(Resource.Id.github_link);
            ircf = view.FindViewById<LinearLayout>(Resource.Id.ircf_link);

            github.Click += Github_Click;
            ircf.Click += IRCF_Click;
            return view;
        }

        private async void Github_Click(object sender, EventArgs e)
        {
            await Browser.OpenAsync("https://github.com/kazem-ma79/CFScanner", BrowserLaunchMode.SystemPreferred);
        }

        private async void IRCF_Click(object sender, EventArgs e)
        {
            await Browser.OpenAsync("https://ircf.space", BrowserLaunchMode.SystemPreferred);
        }
    }
}