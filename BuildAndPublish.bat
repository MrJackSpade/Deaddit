@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

IF /I "%1"=="preview" (
    call BuildPreview.bat
) ELSE (
    call BuildRelease.bat
)

REM Read the version that was built
SET /P Version=<BuildVersion.dat

REM Tag and push
git tag %Version%
git push origin %Version%

IF /I "%1"=="preview" (
    call PublishPreview.bat
) ELSE (
    call PublishRelease.bat
    powershell -ExecutionPolicy Bypass -File PostRelease.ps1
)

REM Cleanup temp file
del BuildVersion.dat

pause
