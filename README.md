# How to install
Android APK files are on the [Releases](/../../releases) page. Every release is built and signed by GitHub Actions. The release marked **Latest** is the stable build; **Pre-release** entries are previews.

Download the APK file and install it on your Android device.

# How to Log In

No API credentials or app registration required. On first launch, tap the profile button and log in through the Reddit login page. Your session is saved automatically.

# Building

Deaddit is a Kotlin (Jetpack Compose) Android app. Reddit access is the C# [Reddit.Api](https://github.com/MrJackSpade/Reddit.Api) library (a submodule; clone with `--recursive`), compiled with .NET NativeAOT into `libredditapi.so` by `native/publish-android.sh`, which needs Linux, the .NET 10 SDK and the Android NDK. Then `./gradlew :app:assembleDebug`.
