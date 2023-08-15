using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFScanner
{
    public static class Helper
    {
        public static string GetStringFromResources(this Context context, int resId)
        {
            return context.GetString(resId);
        }

        public static List<string> GetCDNIPList(this AssetManager assetManager)
        {
            using Stream stream = assetManager.Open("cdn.txt");
            using StreamReader sr = new StreamReader(stream);
            string fileContents = sr.ReadToEnd();
            string[] cdnIps = fileContents.Split("\r\n");
            return new List<string>(cdnIps);
        }
    }
}
