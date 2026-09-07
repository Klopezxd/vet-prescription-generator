@echo off
title Compilando Recetario Agrocalidad (C# .NET 10 Nativo)
chcp 65001 >nul
color 0A

echo ========================================================
echo   Compilando Recetario Agrocalidad en Ejecutable (.exe)
echo   Arquitectura: C# .NET 10 (Single-File Trimmed AOT)
echo ========================================================
echo.

echo [1/3] Verificando y cerrando instancias activas del recetario...
taskkill /F /IM Recetario_Agrocalidad.exe >nul 2>&1
ping -n 2 127.0.0.1 >nul

echo [2/3] Generando binario autonomo con dotnet publish...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist

if %ERRORLEVEL% equ 0 (
    echo.
    echo [3/3] Sincronizando con carpeta de trabajo en el Escritorio...
    if exist "C:\Users\lklev\Desktop\Receta" (
        copy /Y "dist\Recetario_Agrocalidad.exe" "C:\Users\lklev\Desktop\Receta\Recetario_Agrocalidad.exe" >nul
        echo   [OK] Copiado a: C:\Users\lklev\Desktop\Receta\Recetario_Agrocalidad.exe
    )
    echo.
    echo ========================================================
    echo   [EXITO] Ejecutable nativo generado en: dist\Recetario_Agrocalidad.exe
    echo ========================================================
) else (
    echo.
    echo [ERROR] Hubo un problema durante la compilacion.
)
pause
