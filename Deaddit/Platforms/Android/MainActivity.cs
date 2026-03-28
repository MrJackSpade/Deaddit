using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Deaddit.Core.Configurations.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            if (IsKeepAliveEnabled())
            {
                RequestNotificationPermission();
                StartKeepAliveService();
            }
        }

        private static bool IsKeepAliveEnabled()
        {
            string json = Preferences.Get(typeof(ApplicationHacks).FullName!, "");

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                JsonSerializerOptions options = new();
                options.Converters.Add(new JsonStringEnumConverter());
                ApplicationHacks? hacks = JsonSerializer.Deserialize<ApplicationHacks>(json, options);
                return hacks?.KeepAlive == true;
            }
            catch
            {
                return false;
            }
        }

        private void RequestNotificationPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu &&
                ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.PostNotifications) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, [global::Android.Manifest.Permission.PostNotifications], 0);
            }
        }

        public static void StartKeepAliveService()
        {
            Intent intent = new(Platform.AppContext, typeof(KeepAliveService));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                Platform.AppContext.StartForegroundService(intent);
            }
            else
            {
                Platform.AppContext.StartService(intent);
            }
        }

        public static void StopKeepAliveService()
        {
            Intent intent = new(Platform.AppContext, typeof(KeepAliveService));
            Platform.AppContext.StopService(intent);
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
