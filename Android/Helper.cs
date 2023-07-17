using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CFScanner
{
    public static class Helper
    {
        public static string GetStringFromResources(this Android.Content.Context context, int resId)
        {
            return context.GetString(resId);
        }
    }
}