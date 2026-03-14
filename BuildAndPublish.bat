@echo off
call BuildRelease.bat
call PublishRelease.bat

REM Tag the release with the version
SET /P Version=<Version.dat
git tag %Version%
git push origin %Version%

powershell -ExecutionPolicy Bypass -File PostRelease.ps1
pause