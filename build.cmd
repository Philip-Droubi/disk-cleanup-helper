@echo off
rem Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
rem Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
rem Builds the next version of Disk Cleanup Helper (1.0, 1.1, 1.2 ...) into builds\v<version>\.
rem Add -Rebuild to build the current version again, -Major for the next major version, -Publish to publish it.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
echo.
pause
