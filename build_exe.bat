@echo off
title Compilando Recetario Agrocalidad (C# .NET 10 Nativo)
chcp 65001 >nul
color 0A

echo ========================================================
echo   Compilando Recetario Agrocalidad en Ejecutable (.exe)
echo   Arquitectura: C# .NET 10 (Single-File Trimmed AOT)
echo ========================================================
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================
    echo   [EXITO] Ejecutable nativo generado en: dist\Recetario_Agrocalidad.exe
    echo ========================================================
) else (
    echo.
    echo [ERROR] Hubo un problema durante la compilacion.
)
pause
