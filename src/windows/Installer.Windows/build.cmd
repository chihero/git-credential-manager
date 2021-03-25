@ECHO OFF
SETLOCAL enableextensions enabledelayedexpansion

REM Arguments
SET CONFIGURATION=%1
SET ISCC=%2

IF "%CONFIGURATION%"=="" (
	SET CONFIGURATION=Debug
)

IF "%ISCC%"=="" (
	ECHO Missing required ISCC argument [position 2]
	EXIT 1
) ELSE IF NOT EXIST "%ISCC%" (
	ECHO Failed to locate Inno setup compiler: %ISCC%
	EXIT 1
)

ECHO Building Installer.Windows...

REM Directories
SET ROOT=%~dp0..\..\..
SET OUTPUT=%ROOT%\out\windows\Installer.Windows\pkg\%CONFIGURATION%
SET PAYLOAD=%OUTPUT%\payload

CALL %~dp0\layout.cmd %CONFIGURATION% %PAYLOAD%
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

CALL %~dp0\pack.cmd %PAYLOAD% %OUTPUT% %ISCC%
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

ECHO Build of Installer.Windows complete.
