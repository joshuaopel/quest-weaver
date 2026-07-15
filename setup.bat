@echo off
:: Quest Weaver — one-click tester setup.
:: Installs Ollama if missing, lets you pick a model, opens the tool.
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

:: ---- 2. Pick and pull a model (skips download instantly if already present) ----
echo Which AI model should I download?
echo.
echo   [1] Gemma 4 e4b  - BEST writing   (9.6 GB download, ~10 GB RAM or a gaming GPU)
echo   [2] Gemma 4 e2b  - good writing   (7.2 GB download, mid-range PCs)
echo   [3] Qwen 2.5 3b  - light ^& fast   (1.9 GB download, weak PCs / small SSDs)
echo.
choice /c 123 /n /t 60 /d 1 /m "Press 1, 2 or 3 (picks 1 automatically in 60s): "
if errorlevel 3 ( set "MODEL=qwen2.5:3b" ) else if errorlevel 2 ( set "MODEL=gemma4:e2b" ) else ( set "MODEL=gemma4:e4b" )
echo.
echo Downloading %MODEL% (one time - grab a coffee if you picked Gemma)...
"%OLLAMA_EXE%" pull %MODEL%
if errorlevel 1 (
  echo.
  echo Model download failed. Check your internet connection and run this
  echo file again. If it keeps failing, see SETUP.md - Troubleshooting.
  pause
  exit /b 1
)
echo.
echo Model: OK  (%MODEL%)
echo You can grab more models later with the download button inside the tool.
echo.

:: ---- 3. Launch the tool ----
echo Opening Quest Weaver...
call "%~dp0run.bat"
