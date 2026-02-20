@echo off
setlocal EnableDelayedExpansion
::----------------------------------------------
:: GitHub Release Automation Script
::----------------------------------------------

:: Set your GitHub repository details
set REPO_OWNER=MrJackSpade
set REPO_NAME=Deaddit

::----------------------------------------------
:: Read and trim version from Version.dat
::----------------------------------------------
if not exist Version.dat (
    echo Error: Version.dat file not found.
    goto :eof
)

:: Use PowerShell to read and trim the version string
for /f "delims=" %%A in ('powershell -NoProfile -Command "Get-Content -Path 'Version.dat' | ForEach-Object { $_.Trim() }"') do (
    set "TAG_NAME=%%A"
)

:: Check if TAG_NAME is empty after trimming
if "%TAG_NAME%"=="" (
    echo Error: TAG_NAME is empty after trimming. Please check Version.dat.
    goto :eof
)

set "RELEASE_NAME=Release %TAG_NAME%"

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
set RELEASE_BODY=Automated Release

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
    echo     "prerelease": false,
    echo     "make_latest": "true"
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

echo Release created and files uploaded successfully!
endlocal
