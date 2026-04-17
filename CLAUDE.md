# CLAUDE.md

## Building

The Deaddit MAUI project **cannot** be built from WSL `dotnet` directly — the NuGet config has Windows-baked fallback paths (`C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`) that fail to resolve under Linux. `Deaddit.Core` builds fine from WSL; the MAUI head does not.

**Compile-check the MAUI project from WSL via Windows cmd** (no publish, just verify it builds):

```bash
/mnt/c/Windows/System32/cmd.exe /c "cd /d Z:\git\Deaddit && dotnet build Deaddit\Deaddit.csproj -f net10.0-android"
```

Do this whenever you make code changes that touch the `Deaddit/` MAUI project — you can verify compilation without bothering the user. You don't need explicit permission to run a build; only publishing requires permission.

## Deployment

To build and publish for testing, run the full BuildAndPublish script from WSL:

```bash
/mnt/c/Windows/System32/cmd.exe /c "cd /d Z:\git\Deaddit && BuildAndPublish.bat"
```

This builds the APK, creates a GitHub release, and makes it available to pull to the user's phone. Publishing IS the testing step — the user is the only one using the app.

## Workflow

- Build and publish first so the user can test on their phone.
- Never commit code for an issue until the user has tested the fix and confirmed it works.
- If the app ever gets other users, the publish-before-commit flow will need to change.
