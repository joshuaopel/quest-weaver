@echo off
:: Quest Weaver launcher — double-click to serve the tool and open it.
:: Starts Ollama if it isn't already running, serves this folder on
:: http://localhost:9999 and opens the browser.

cd /d "%~dp0"

:: nudge Ollama awake (harmless if already running; ignore errors if not installed here)
tasklist | findstr /i "ollama app.exe" >nul 2>&1
if errorlevel 1 (
  if exist "D:\Ollama\ollama app.exe" start "" "D:\Ollama\ollama app.exe"
  if exist "%LOCALAPPDATA%\Programs\Ollama\ollama app.exe" start "" "%LOCALAPPDATA%\Programs\Ollama\ollama app.exe"
)

start "" http://localhost:9999
echo Quest Weaver running at http://localhost:9999  (close this window to stop)
python -m http.server 9999
