@echo off
set "UNITY_EDITOR=E:\Unity\Editors\6000.0.62f1\Editor\Unity.exe"
if not exist "%UNITY_EDITOR%" (
  echo Unity Editor not found. Open this folder using Unity Hub.
  pause
  exit /b 1
)
start "" "%UNITY_EDITOR%" -projectPath "%~dp0." -force-d3d11
