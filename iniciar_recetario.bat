@echo off
title Recetario Veterinario - Agrocalidad Ecuador (C# .NET 10)
chcp 65001 >nul
color 0B

echo ========================================================
echo   Iniciando Sistema de Recetas Veterinarias (Agrocalidad)
echo   Plataforma: C# .NET 10 Nativo de Alto Rendimiento
echo ========================================================
echo.

dotnet run

if %ERRORLEVEL% neq 0 (
    echo.
    echo [AVISO] Se cerr? la aplicaci?n o ocurri? un problema.
    pause
)
