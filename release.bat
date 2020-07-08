@echo off

ECHO.
ECHO This release script requires powershell to be runnable from cmd.
ECHO.

IF %1.==. GOTO VersionError
set version=%1
set buildpath=.\CDPBatchEditor\bin\Release\netcoreapp3.1\*
set filename=CDP4_BatchEditor_Community_Edition_%version%.zip

GOTO Setup

:VersionError
ECHO.
ECHO ERROR: No version was specified
ECHO.

GOTO End

:Setup

ECHO Releasing CDPBatchEditor Version %version%

set dry=false

IF %2.==. GOTO Begin
IF %2==dry GOTO Dry

:Dry

ECHO.
ECHO Performing dry run...
ECHO.

set dry=true

:Begin

ECHO.
ECHO Building Release Version
ECHO.

call dotnet build -c Release -v q

ECHO Error Level %errorlevel%

IF %errorlevel%==1 GOTO BuildError

ECHO.
ECHO Running Unit Tests
ECHO.

call dotnet test -c Release -v q

ECHO Error Level %errorlevel%

IF %errorlevel%==1 GOTO BuildError

IF %dry%==true GOTO End

ECHO.
ECHO Building Release Archive
ECHO.

call powershell -Command "& {Compress-Archive -Path %buildpath% -DestinationPath %filename% -Force;}"

zip -r -p "%filename%" "%buildpath%\*"

:End

ECHO.
ECHO Release %version% Completed
ECHO.

EXIT /B 0

:BuildError

ECHO.
ECHO Release %version% Failed
ECHO.

EXIT /B 1