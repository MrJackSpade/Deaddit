using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

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
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Workaround for MAUI 10 bug where the system navigation bar overlaps app content
            // after the soft keyboard opens and closes. Uses GetInsetsIgnoringVisibility to ensure
            // correct system bar padding is always applied regardless of keyboard state.
            // Remove when fixed upstream: https://github.com/dotnet/maui/issues/33237
            Window?.DecorView.SetOnApplyWindowInsetsListener(new SystemBarInsetsListener());
        }

        private class SystemBarInsetsListener : Java.Lang.Object, global::Android.Views.View.IOnApplyWindowInsetsListener
        {
            public WindowInsets OnApplyWindowInsets(global::Android.Views.View v, WindowInsets insets)
            {
                var sysBars = insets.GetInsetsIgnoringVisibility(WindowInsets.Type.SystemBars());
                v.SetPadding(sysBars.Left, sysBars.Top, sysBars.Right, sysBars.Bottom);
                return WindowInsets.Consumed;
            }
        }
    }
}