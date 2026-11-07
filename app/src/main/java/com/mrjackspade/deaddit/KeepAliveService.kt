package com.mrjackspade.deaddit

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder

/**
 * "Keep Alive (Background Service)": a silent foreground notification that keeps the process,
 * and so the app's state, from being reclaimed in the background (the C# KeepAliveService).
 */
class KeepAliveService : Service() {
    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        createNotificationChannel()

        // The launcher icon, which the C# notification used; this app has no icon of its own yet.
        val notification = Notification.Builder(this, CHANNEL_ID)
            .setContentTitle("Deaddit")
            .setContentText("Running")
            .setSmallIcon(android.R.drawable.sym_def_app_icon)
            .setOngoing(true)
            .build()

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.UPSIDE_DOWN_CAKE) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }

        return START_STICKY
    }

    private fun createNotificationChannel() {
        // Low importance: no sound, as the C# SetSilent(true).
        val channel = NotificationChannel(CHANNEL_ID, "Background Service", NotificationManager.IMPORTANCE_LOW).apply {
            description = "Keeps Deaddit running in the background"
        }
        getSystemService(NotificationManager::class.java).createNotificationChannel(channel)
    }

    companion object {
        private const val NOTIFICATION_ID = 9999
        private const val CHANNEL_ID = "deaddit_keepalive"

        fun start(context: Context) {
            context.startForegroundService(Intent(context, KeepAliveService::class.java))
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, KeepAliveService::class.java))
        }
    }
}
