# CLAUDE.md

## The app

Deaddit is a native Android app: Kotlin + Jetpack Compose, Gradle (`app/` UI, `core/` pure-Kotlin logic with unit tests). Reddit access is the C# `Reddit.Api` library (submodule), compiled with NativeAOT into `libredditapi.so` and called through a JSON bridge: `native/RedditApi.Native` (C# exports and commands), `app/src/main/cpp/bridge.c` (JNI), `app/.../api/NativeRedditApi.kt` (Kotlin side; OkHttp does the HTTP for .NET, which has no TLS on Android). The old MAUI app was deleted after the port (find it in git history before commit "Delete the C# app"); the Kotlin app has the same package ID and migrates the data it stored (`settings/MauiMigration.kt`).

## Building

- Kotlin: `gradlew.bat --no-daemon :app:assembleDebug` and `:core:test`. Run long builds in the background.
- `libredditapi.so` (gitignored, in `app/src/main/jniLibs/<abi>/`): .NET can only build it on Linux, so locally `bash native/build-android.sh [arm64-v8a] [x86_64]` runs `native/publish-android.sh` in the QEMU builder VM (`G:\VMs\deaddit-linux-builder`, see its README; start it with its `start.ps1`). Rebuild it after any change to `Reddit.Api` or `native/`. Never edit `build-android.sh` while it's running (bash reads scripts as it goes).
- NativeAOT can't do reflection over generic types nothing references at compile time: Reddit.Api's JSON goes through `Reddit.Api/Json/RedditJsonContext.cs` there, so add new model types to it.

## Reproducing bugs — drive the app yourself

For ANY app bug, reproduce it on the dev emulator (`emulator-5554`, AVD `deaddit-dev`, see `G:\VMs\deaddit-android-emulator`) before diagnosing. Never ask the user to run it, never guess from code. The RP6 is the user's device: use it only for test rounds they set up. Never touch `192.168.1.118` (an unrelated RG DS).

- Test mode: `adb shell run-as com.companyname.deaddit touch files/test-mode` — errors go to logcat instead of dialogs and nothing opens the login page. Read `adb logcat` (tags `Deaddit`, `RedditApi`, `RealImageLoader`, `AndroidRuntime`): every failed request and error is logged with its full .NET or JVM stack.
- Drive it with `adb shell input tap/swipe`, find elements with `uiautomator dump` (icon buttons have content descriptions), and save screenshots with `adb exec-out screencap -p` to a real path.
- Debug builds expose WebViews (login, browser, image viewer) to DevTools: `adb forward tcp:9333 localabstract:webview_devtools_remote_<pid>`, then the DevTools protocol on `http://127.0.0.1:9333/json`.
## Reddit endpoints

Reddit keeps blocking endpoints with a 403 "blocked by network security" page. `Reddit.Api/Reddit.Api/Client` is split by host into `OAuth/`, `Www/` and `Old/`, and each partial's header comment records what happened and why its calls are there. Reddit is closing the public API (access starts closing 2027-01-12, closes for everyone in March 2027), so calls are moving off OAuth; see `Client/OAuth/RedditClient.OAuth.cs` for what's left. When something breaks, read those first and check `www` first, for parity with Reddit's main site. Probe with a throwaway test using the stored session, and clear any change of endpoint with the user before making it.

## Deployment

Publishing runs entirely in GitHub Actions (`.github/workflows/publish.yml`). It builds `libredditapi.so` for each ABI, runs the core tests, builds the signed APK, computes the version from existing tags, creates the GitHub release (which creates the tag), and — for `release` only — posts to r/DeadditApp. Trigger it and follow it with:

```bash
gh workflow run publish.yml --ref main -f channel=preview
gh workflow run publish.yml --ref main -f channel=release
gh run watch
```

The workflow builds what is on GitHub, so commit and push to `main` first. Publishing IS the testing step — the user is the only one using the app.

Signing and Reddit credentials live only in repository secrets (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `ANDROID_KEYSTORE_PASSWORD`, `REDDIT_CLIENT_ID`, `REDDIT_CLIENT_SECRET`, `REDDIT_USERNAME`, `REDDIT_PASSWORD`). Never commit a keystore or credential file.

## Workflow

- Everything goes to `main`, always. No feature branches.
- Commit and push to `main`, then publish a preview so the user can test on their phone.
