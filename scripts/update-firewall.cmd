@echo off
REM Convenience wrapper so the script can be run from cmd.exe or by double-click.
REM All arguments are forwarded, e.g.:  update-firewall.cmd -WhatIf
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0update-firewall.ps1" %*
