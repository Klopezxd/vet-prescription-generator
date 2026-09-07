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

:: 2. Abrir automáticamente en el navegador predeterminado
start "" http://localhost:8000

echo [OK] Servidor iniciado en http://localhost:8000
echo Para acceder desde tu celular o tablet en el local:
echo Abre la IP local de esta PC con el puerto :8000
echo.
echo Presiona Ctrl+C para cerrar el sistema.
echo ========================================================
echo.

:: 3. Iniciar servidor FastAPI con Uvicorn
python -m uvicorn src.api:app --host 0.0.0.0 --port 8000

pause
