# CLAUDE.md

## Building

The Deaddit MAUI project **cannot** be built from WSL `dotnet` directly — the NuGet config has Windows-baked fallback paths (`C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`) that fail to resolve under Linux. `Deaddit.Core` builds fine from WSL; the MAUI head does not.

**Compile-check the MAUI project from WSL via Windows cmd** (no publish, just verify it builds):

```bash
/mnt/c/Windows/System32/cmd.exe /c "cd /d Z:\git\Deaddit && dotnet build Deaddit\Deaddit.csproj -f net10.0-android"
```

Do this whenever you make code changes that touch the `Deaddit/` MAUI project — you can verify compilation without bothering the user. You don't need explicit permission to run a build; only publishing requires permission.

## Reproducing bugs — drive the Windows app yourself

The app runs natively on Windows. For ANY app bug (render issue, API error dialog, crash), build the Windows head (`dotnet build Deaddit\Deaddit.csproj -f net10.0-windows10.0.19041.0 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true`; without those flags the exe crashes on launch), launch it, and drive it YOURSELF to reproduce before diagnosing. Never say you can't run the app, never ask the user to run it, never guess from code, never look for Android emulators.

Always launch it in test mode when driving it: `DEADDIT_TEST_MODE=1` on the launched process, with stderr redirected to a file (`Start-Process -RedirectStandardError`). The user's desktop is in use, so errors must go to the log, never to a dialog on their screen; test mode also stops a rejected session opening the login page. Every failed request and displayed error is logged, so read the log rather than inferring from the screen.

Computer-use tools are not required. Drive it from the shell: WebView2 remote debugging (`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9222 --remote-allow-origins=http://localhost`, set only on the launched process, not at user scope) + DevTools protocol for the WebView2-rendered posts/comments, PowerShell UI Automation for native controls, and `PrintWindow` screenshots saved to a real path. Click the actual element (a "More" sits inside a reply container with its own `onclick` and the same text), ideally with `Input.dispatchMouseEvent`. The app uses the stored logged-in session itself, so this is how live-session API calls get exercised.

## Reddit endpoints

Reddit keeps blocking endpoints with a 403 "blocked by network security" page. `Reddit.Api/Reddit.Api/Client` is split by host into `OAuth/`, `Www/` and `Old/`, and each partial's header comment records what happened and why its calls are there. Reddit is closing the public API (access starts closing 2027-01-12, closes for everyone in March 2027), so calls are moving off OAuth; see `Client/OAuth/RedditClient.OAuth.cs` for what's left. When something breaks, read those first and check `www` first, for parity with Reddit's main site. Probe with a throwaway test using the stored session, and clear any change of endpoint with the user before making it.

## Deployment

Publishing runs entirely in GitHub Actions (`.github/workflows/publish.yml`). It builds the signed APK, computes the version from existing tags, creates the GitHub release (which creates the tag), and — for `release` only — posts to r/DeadditApp. Trigger it and follow it with:

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
