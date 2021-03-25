@ECHO OFF
SETLOCAL enableextensions enabledelayedexpansion

REM Arguments
SET CONFIGURATION=%1
SET PAYLOADOUT=%2
SET SYMBOLOUT=%3

IF "%CONFIGURATION%"=="" (
	SET CONFIGURATION=Debug
)

IF "%PAYLOADOUT%"=="" (
	ECHO Missing required output directory argument [position 2]
	EXIT 1
)

IF "%SYMBOLOUT%"=="" (
	SET SYMBOLOUT=%PAYLOADOUT%.sym
)

REM Directories
SET ROOT=%~dp0..\..\..
SET SRC=%ROOT%\src
SET GCMSRC=%SRC%\shared\Git-Credential-Manager
SET BITBUCKETUISRC=%SRC%\windows\Atlassian.Bitbucket.UI.Windows
SET GITHUBUISRC=%SRC%\windows\GitHub.UI.Windows

REM Build parameters
SET FRAMEWORK=net5.0-windows10.0.17763.0
SET RUNTIME=win-x86

REM Cleanup any old output directories
IF EXIST %PAYLOADOUT% (
	ECHO Cleaning old payload directory '%PAYLOADOUT%'...
	RMDIR /s /q %PAYLOADOUT%
)

IF EXIST %SYMBOLOUT% (
	ECHO Cleaning old symbol directory '%SYMBOLOUT%'...
	RMDIR /s /q %SYMBOLOUT%
)

REM Ensure payload and symbol directories exists
MKDIR %PAYLOADOUT%
MKDIR %SYMBOLOUT%

REM Publish application executables
ECHO Publishing core application...
dotnet publish "%GCMSRC%" ^
	--configuration="%CONFIGURATION%" ^
	--framework="%FRAMEWORK%" ^
	--runtime="%RUNTIME%" ^
	--self-contained=true ^
	-p:PublishSingleFile=true ^
	-p:IncludeNativeLibrariesForSelfExtract=true ^
	-p:PublishTrimmed=true ^
	--output="%PAYLOADOUT%"
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

ECHO Publishing Bitbucket UI helper...
dotnet publish "%BITBUCKETUISRC%" ^
	--configuration="%CONFIGURATION%" ^
	--framework="%FRAMEWORK%" ^
	--runtime="%RUNTIME%" ^
	--self-contained=true ^
	-p:PublishSingleFile=true ^
	-p:IncludeNativeLibrariesForSelfExtract=true ^
	-p:PublishTrimmed=true ^
	--output="%PAYLOADOUT%"
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

ECHO Publishing GitHub UI helper...
dotnet publish "%GITHUBUISRC%" ^
	--configuration="%CONFIGURATION%" ^
	--framework="%FRAMEWORK%" ^
	--runtime="%RUNTIME%" ^
	--self-contained=true ^
	-p:PublishSingleFile=true ^
	-p:IncludeNativeLibrariesForSelfExtract=true ^
	-p:PublishTrimmed=true ^
	--output="%PAYLOADOUT%"
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

REM Collect symbols
ECHO Collecting managed symbols...
MOVE %PAYLOADOUT%\*.pdb %SYMBOLOUT%
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

ECHO Layout complete.
