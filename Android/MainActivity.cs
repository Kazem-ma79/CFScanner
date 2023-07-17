using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;

namespace CFScanner
{
    [Activity(Label = "CFScanner", Theme = "@style/AppTheme", MainLauncher = true)]

    public class MainActivity : AppCompatActivity, BottomNavigationView.IOnNavigationItemSelectedListener
    {
        private BottomNavigationView navigation;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            navigation = FindViewById<BottomNavigationView>(Resource.Id.navigation);
            navigation.SetOnNavigationItemSelectedListener(this);

            LoadPage(Resource.Id.navigation_scanner);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.navigation_scanner:
                    LoadPage(Resource.Id.navigation_scanner);
                    return true;
                case Resource.Id.navigation_checker:
                    LoadPage(Resource.Id.navigation_checker);
                    return true;
                case Resource.Id.navigation_about:
                    LoadPage(Resource.Id.navigation_about);
                    return true;
            }
            return false;
        }

        private void LoadPage(int pageId)
        {
            AndroidX.Fragment.App.Fragment fragment;

            switch (pageId)
            {
                case Resource.Id.navigation_scanner:
                    fragment = new Fragments.Scanner();
                    break;
                case Resource.Id.navigation_checker:
                    fragment = new Fragments.Checker();
                    break;
                case Resource.Id.navigation_about:
                    fragment = new Fragments.About();
                    break;
                default:
                    return;
            }

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.frameContent, fragment)
                .Commit();
        }

    }
}