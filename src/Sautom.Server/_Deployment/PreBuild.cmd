@ECHO OFF

SET TargetPath=%~1
SET SvcName=WCFWindowsHostService

SETLOCAL ENABLEDELAYEDEXPANSION

ECHO STOP SERVICE:
net stop %SvcName%

REM ECHO UNISTALL:
REM installutil /u %TargetPath%