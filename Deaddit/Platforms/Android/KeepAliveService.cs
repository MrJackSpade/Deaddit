using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace Deaddit.Platforms.Android
{
    [Service(ForegroundServiceType = ForegroundService.TypeSpecialUse, Exported = false)]
    public class KeepAliveService : Service
    {
        private const int NotificationId = 9999;
        private const string ChannelId = "deaddit_keepalive";

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();

            Notification notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("Deaddit")
                .SetContentText("Running")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .SetSilent(true)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
            {
                StartForeground(NotificationId, notification, ForegroundService.TypeSpecialUse);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }

            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel channel = new(ChannelId, "Background Service", NotificationImportance.Low)
                {
                    Description = "Keeps Deaddit running in the background"
                };

                NotificationManager? manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }
    }
}
