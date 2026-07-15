@echo off
:: Quest Weaver — one-click tester setup.
:: Installs Ollama if missing, downloads the small test model, opens the tool.
setlocal

echo ============================================
echo  Quest Weaver setup
echo ============================================
echo.

:: ---- 1. Ollama installed? ----
set "OLLAMA_EXE=ollama"
where ollama >nul 2>&1
if errorlevel 1 (
  if exist "%LOCALAPPDATA%\Programs\Ollama\ollama.exe" (
    set "OLLAMA_EXE=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
  ) else (
    echo Ollama not found - installing it now ^(free, no admin needed^)...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "irm https://ollama.com/install.ps1 | iex"
    if exist "%LOCALAPPDATA%\Programs\Ollama\ollama.exe" (
      set "OLLAMA_EXE=%LOCALAPPDATA%\Programs\Ollama\ollama.exe"
    ) else (
      echo.
      echo Ollama install did not finish. Install it manually from
      echo   https://ollama.com/download
      echo then run this file again.
      pause
      exit /b 1
    )
  )
)
echo Ollama: OK
echo.

:: ---- 2. Pull the small test model (skips instantly if already present) ----
echo Downloading the test model qwen2.5:0.5b (~0.4 GB, one time)...
"%OLLAMA_EXE%" pull qwen2.5:0.5b
if errorlevel 1 (
  echo.
  echo Model download failed. Check your internet connection and run this
  echo file again. If it keeps failing, see SETUP.md - Troubleshooting.
  pause
  exit /b 1
)
echo.
echo Model: OK
echo Tip: for much better writing (9.6 GB download, needs ~10 GB VRAM/RAM):
echo    ollama pull gemma4:e4b
echo.

:: ---- 3. Launch the tool ----
echo Opening Quest Weaver...
call "%~dp0run.bat"
