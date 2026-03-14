# CLAUDE.md

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
