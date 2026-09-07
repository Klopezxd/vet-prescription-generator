@echo off
title Recetario Veterinario - Agrocalidad Ecuador
chcp 65001 >nul
color 0B

echo ========================================================
echo   Iniciando Sistema de Recetas Veterinarias (Agrocalidad)
echo ========================================================
echo.

:: 1. Activar entorno virtual si existe
if exist "venv\Scripts\activate.bat" (
    call "venv\Scripts\activate.bat"
) else if exist ".venv\Scripts\activate.bat" (
    call ".venv\Scripts\activate.bat"
)

:: 2. Iniciar aplicación nativa de escritorio
python main.py

if %ERRORLEVEL% neq 0 (
    echo.
    echo [AVISO] Se cerró la aplicación o ocurrió un problema.
    pause
)
