@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

REM Check if Version.Dat exists, if not, create it with 0.0.0.0
IF NOT EXIST Version.Dat (
    ECHO 0.0.0 > Version.Dat
)

REM Read the current version from Version.Dat
SET /P Version=<Version.Dat

REM Initialize version components
SET Major=0
SET Minor=0
SET Build=0

REM Parse the version string into components
FOR /F "tokens=1-4 delims=." %%A IN ("%Version%") DO (
    SET Major=%%A
    SET Minor=%%B
    SET Build=%%C
)

REM Ensure all components are set
IF "%Minor%"=="" SET Minor=0
IF "%Build%"=="" SET Build=0

REM Increment the build number
SET /A Build=%Build%+1

REM Construct the new version string
SET NewVersion=%Major%.%Minor%.%Build%

REM Write the new version to Version.Dat
ECHO %NewVersion% > Version.Dat

REM Display the new version
ECHO New Version: %NewVersion%

REM Proceed with the build
dotnet build Deaddit -f net9.0-android -c Release --artifacts-path Release

REM Cleanup
ECHO Cleaning up...
move Release\bin\Deaddit\release_net9.0-android\com.companyname.deaddit-Signed.apk Release\Deaddit-%NewVersion%.apk
rmdir /s /Q Release\bin
rmdir /s /Q Release\obj