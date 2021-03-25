@ECHO OFF
SETLOCAL enableextensions enabledelayedexpansion

REM Arguments
SET PAYLOAD=%1
SET SETUPOUT=%2
SET ISCC=%3

IF "%PAYLOAD%"=="" (
	ECHO Missing required payload directory argument [position 1]
	EXIT 1
)

IF "%SETUPOUT%"=="" (
	ECHO Missing required output directory argument [position 2]
	EXIT 1
)

IF "%ISCC%"=="" (
	ECHO Missing required ISCC argument [position 3]
	EXIT 1
) ELSE IF NOT EXIST "%ISCC%" (
	ECHO Failed to locate Inno setup compiler: %ISCC%
	EXIT 1
)

ECHO Packing system installer...
CALL %ISCC% /DPayloadDir=%PAYLOAD% /DInstallTarget=system Setup.iss /O%SETUPOUT%
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

ECHO Packing user installer...
CALL %ISCC% /DPayloadDir=%PAYLOAD% /DInstallTarget=user Setup.iss /O%SETUPOUT%
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

ECHO Pack complete.
