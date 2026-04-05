@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

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

REM Preview uses next build number without persisting
SET /A Build=%Build%+1
SET NewVersion=%Major%.%Minor%.%Build%

REM Read and increment preview counter
SET Preview=0
IF EXIST Preview.dat (
    SET /P Preview=<Preview.dat
)
SET /A Preview=%Preview%+1
ECHO %Preview% > Preview.dat

SET FullVersion=%NewVersion%-preview-%Preview%

REM Write build version for BuildAndPublish to read
ECHO %FullVersion% > BuildVersion.dat

REM Display the version
ECHO Preview Version: %FullVersion%

REM Proceed with the build
dotnet build Deaddit -f net10.0-android -c Release --artifacts-path Release -p:DeadditVersion=%FullVersion%

REM Cleanup
ECHO Cleaning up...
move Release\bin\Deaddit\release_net10.0-android\com.companyname.deaddit-Signed.apk Release\Deaddit-%FullVersion%.apk
rmdir /s /Q Release\bin
rmdir /s /Q Release\obj
