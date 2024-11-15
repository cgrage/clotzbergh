@echo off
REM Remove the target folder recursively
rmdir /S /Q "..\Clotzbergh Player 2\Assets"

REM Copy the source folder to the target location
xcopy /E /I /H ".\Assets" "..\Clotzbergh Player 2\Assets"

echo Folder copied successfully.
