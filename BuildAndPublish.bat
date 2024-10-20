@echo off
call BuildRelease.bat
call PublishRelease.bat
powershell -ExecutionPolicy Bypass -File PostRelease.ps1
pause