@echo off
call BuildRelease.bat

REM Tag the release with the version before publishing
REM (PublishRelease creates the tag on GitHub via API, so push first)
SET /P Version=<Version.dat
git tag %Version%
git push origin %Version%

call PublishRelease.bat

powershell -ExecutionPolicy Bypass -File PostRelease.ps1
pause