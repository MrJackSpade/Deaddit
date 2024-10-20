::@echo off
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

set RELEASE_NAME=Release %TAG_NAME%

::----------------------------------------------
:: Read PAT from Pat.dat
::----------------------------------------------
if not exist Pat.dat (
    echo Error: Pat.dat file not found.
    goto :eof
)
set /p GITHUB_TOKEN=<Pat.dat

:: Release details
set RELEASE_BODY=Automated Release

:: Files to upload (space-separated list)
set FILES_TO_UPLOAD=Release\Deaddit.apk

::----------------------------------------------
:: Create release.json file with release details
::----------------------------------------------
echo Creating release.json...
echo {^
    ^"tag_name^": ^"%TAG_NAME%^",^
    ^"name^": ^"%RELEASE_NAME%^",^
    ^"body^": ^"%RELEASE_BODY%^",^
    ^"draft^": false,^
    ^"prerelease^": false^
} > release.json

::----------------------------------------------
:: Create the release on GitHub
::----------------------------------------------
echo Creating release on GitHub...
curl -s -H "Authorization: token %GITHUB_TOKEN%" ^
     -H "Content-Type: application/json" ^
     -d @release.json ^
     https://api.github.com/repos/%REPO_OWNER%/%REPO_NAME%/releases > release_response.json

::----------------------------------------------
:: Check the response for errors
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
    "($response = Get-Content 'release_response.json' | ConvertFrom-Json).upload_url.Replace('{?name,label}','')"`) do (
    set UPLOAD_URL=%%i
)

::----------------------------------------------
:: Upload each file to the release
::----------------------------------------------
echo Uploading files...
for %%f in (%FILES_TO_UPLOAD%) do (
    echo Uploading %%f...
    curl -s -H "Authorization: token %GITHUB_TOKEN%" ^
         -H "Content-Type: application/octet-stream" ^
         --data-binary @%%f ^
         "%UPLOAD_URL%?name=%%~nxf" > NUL
    echo Uploaded %%f
)

::----------------------------------------------
:: Cleanup temporary files
::----------------------------------------------
del release.json
del release_response.json

echo Release created and files uploaded successfully!
pause
