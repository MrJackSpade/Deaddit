@echo off
setlocal EnableDelayedExpansion
::----------------------------------------------
:: GitHub Preview Release Automation Script
::----------------------------------------------

:: Set your GitHub repository details
set REPO_OWNER=MrJackSpade
set REPO_NAME=Deaddit

::----------------------------------------------
:: Read and trim version from BuildVersion.dat
::----------------------------------------------
if not exist BuildVersion.dat (
    echo Error: BuildVersion.dat file not found.
    goto :eof
)

:: Use PowerShell to read and trim the version string
for /f "delims=" %%A in ('powershell -NoProfile -Command "Get-Content -Path 'BuildVersion.dat' | ForEach-Object { $_.Trim() }"') do (
    set "TAG_NAME=%%A"
)

:: Check if TAG_NAME is empty after trimming
if "%TAG_NAME%"=="" (
    echo Error: TAG_NAME is empty after trimming. Please check BuildVersion.dat.
    goto :eof
)

set "RELEASE_NAME=Preview %TAG_NAME%"

::----------------------------------------------
:: Read and trim PAT from Pat.dat
::----------------------------------------------
if not exist Pat.dat (
    echo Error: Pat.dat file not found.
    goto :eof
)

:: Use PowerShell to read and trim the PAT
for /f "delims=" %%A in ('powershell -NoProfile -Command "Get-Content -Path 'Pat.dat' | ForEach-Object { $_.Trim() }"') do (
    set "GITHUB_TOKEN=%%A"
)

:: Check if GITHUB_TOKEN is empty after trimming
if "%GITHUB_TOKEN%"=="" (
    echo Error: GITHUB_TOKEN is empty after trimming. Please check Pat.dat.
    goto :eof
)

:: Release details
set RELEASE_BODY=Preview Release

:: Files to upload (space-separated list)
set FILES_TO_UPLOAD=Release\Deaddit-%TAG_NAME%.apk

::----------------------------------------------
:: Create release.json file with release details
::----------------------------------------------
echo Creating release.json...
(
    echo {
    echo     "tag_name": "%TAG_NAME%",
    echo     "name": "%RELEASE_NAME%",
    echo     "body": "%RELEASE_BODY%",
    echo     "draft": false,
    echo     "prerelease": true,
    echo     "make_latest": "false"
    echo }
) > release.json

::----------------------------------------------
:: Create the release on GitHub
::----------------------------------------------
echo Creating release on GitHub...
curl -s -H "Authorization: token %GITHUB_TOKEN%" ^
     -H "Content-Type: application/json" ^
     -d @release.json ^
     https://api.github.com/repos/%REPO_OWNER%/%REPO_NAME%/releases > release_response.json

::----------------------------------------------
:: Check for errors in the response
::----------------------------------------------
findstr /m /c:"\"message\": \"Validation Failed\"" release_response.json >nul
if %errorlevel%==0 (
    echo Error: Release creation failed.
    type release_response.json
    goto :eof
)

::----------------------------------------------
:: Extract the upload URL from the response
::----------------------------------------------
echo Extracting upload URL...
for /f "usebackq tokens=*" %%i in (`powershell -NoProfile -Command ^
    "(Get-Content 'release_response.json' | ConvertFrom-Json).upload_url -replace '{\?name,label}', ''"`) do (
    set "UPLOAD_URL=%%i"
)

::----------------------------------------------
:: Upload each file to the release
::----------------------------------------------
echo Uploading files...
for %%f in (%FILES_TO_UPLOAD%) do (
    if exist "%%f" (
        echo Uploading %%f...
        curl -s -H "Authorization: token %GITHUB_TOKEN%" ^
             -H "Content-Type: application/octet-stream" ^
             --data-binary @"%%f" ^
             "%UPLOAD_URL%?name=%%~nxf" > NUL
        echo Uploaded %%f
    ) else (
        echo Warning: File %%f not found. Skipping.
    )
)

::----------------------------------------------
:: Cleanup temporary files
::----------------------------------------------
del release.json
del release_response.json

echo Preview release created and files uploaded successfully!
endlocal
