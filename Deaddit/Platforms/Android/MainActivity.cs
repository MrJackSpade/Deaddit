using Android.App;
using Android.Content.PM;

namespace Deaddit.Platforms.Android
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true, 
        LaunchMode = LaunchMode.SingleTop, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        EnableOnBackInvokedCallback = false)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}