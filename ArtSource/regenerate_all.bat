@echo off
set "BLENDER=C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"

if not exist "%BLENDER%" (
    echo Blender not found at "%BLENDER%".
    echo Edit the BLENDER path at the top of this script if it's installed elsewhere.
    pause
    exit /b 1
)

"%BLENDER%" --background --factory-startup --python "%~dp0regenerate_all.py"
