@echo off
echo Opening Archeforge Unity project in Unity Hub...
echo.
echo If Unity Hub doesn't open automatically, manually:
echo 1. Open Unity Hub
echo 2. Click "Open" -> "Add project from disk"
echo 3. Navigate to: %~dp0
echo 4. Click "Select Folder"
echo.
echo Press any key to continue...
pause > nul

REM Try to open with Unity Hub if installed
start "" "unityhub://%~dp0"

echo.
echo If the project didn't open, make sure Unity Hub is installed.
echo Unity Hub download: https://unity.com/download
pause