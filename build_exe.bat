@echo off
title Compilando Recetario Agrocalidad
chcp 65001 >nul
color 0A

echo ========================================================
echo   Compilando Recetario Agrocalidad en Ejecutable (.exe)
echo ========================================================
echo.

python -m PyInstaller ^
  --name "Recetario_Agrocalidad" ^
  --onefile ^
  --noconsole ^
  --clean ^
  --icon "assets/app_icon.ico" ^
  --add-data "src/templates;src/templates" ^
  --add-data "src/static;src/static" ^
  --add-data "assets;assets" ^
  --hidden-import "uvicorn" ^
  --hidden-import "uvicorn.logging" ^
  --hidden-import "uvicorn.loops" ^
  --hidden-import "uvicorn.loops.auto" ^
  --hidden-import "uvicorn.protocols" ^
  --hidden-import "uvicorn.protocols.http" ^
  --hidden-import "uvicorn.protocols.http.auto" ^
  --hidden-import "uvicorn.protocols.websockets" ^
  --hidden-import "uvicorn.protocols.websockets.auto" ^
  --hidden-import "fastapi" ^
  --hidden-import "jinja2" ^
  --hidden-import "pydantic" ^
  --hidden-import "sqlite3" ^
  --hidden-import "multipart" ^
  --hidden-import "python_multipart" ^
  desktop.py

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================
    echo   [EXITO] Ejecutable generado en: dist\Recetario_Agrocalidad.exe
    echo ========================================================
) else (
    echo.
    echo [ERROR] Hubo un problema durante la compilacion.
)
pause
