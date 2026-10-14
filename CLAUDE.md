# CLAUDE.md

## Building

The Deaddit MAUI project **cannot** be built from WSL `dotnet` directly — the NuGet config has Windows-baked fallback paths (`C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`) that fail to resolve under Linux. `Deaddit.Core` builds fine from WSL; the MAUI head does not.

**Compile-check the MAUI project from WSL via Windows cmd** (no publish, just verify it builds):

```bash
/mnt/c/Windows/System32/cmd.exe /c "cd /d Z:\git\Deaddit && dotnet build Deaddit\Deaddit.csproj -f net10.0-android"
```

Do this whenever you make code changes that touch the `Deaddit/` MAUI project — you can verify compilation without bothering the user. You don't need explicit permission to run a build; only publishing requires permission.

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
