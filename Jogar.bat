@echo off
rem Abre o jogo para jogar.
cd /d "%~dp0"
dotnet build -nologo -v q
start "" "C:\Users\diogo\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path "%~dp0."
