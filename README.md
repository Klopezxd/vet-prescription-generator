# Generador de Recetas Veterinarias (Normativa Agrocalidad Ecuador)

[![GitHub Release](https://img.shields.io/github/v/release/Klopezxd/vet-prescription-generator?label=%C3%9Altima%20Versi%C3%B3n&logo=github&color=blue)](https://github.com/Klopezxd/vet-prescription-generator/releases/latest)
[![CI / Build & QA](https://github.com/Klopezxd/vet-prescription-generator/actions/workflows/lint.yml/badge.svg)](https://github.com/Klopezxd/vet-prescription-generator/actions/workflows/lint.yml)
[![Release CI](https://github.com/Klopezxd/vet-prescription-generator/actions/workflows/release.yml/badge.svg)](https://github.com/Klopezxd/vet-prescription-generator/actions/workflows/release.yml)
![CSharp](https://img.shields.io/badge/.NET-10.0-512BD4.svg?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20(WinExe)-0078D6.svg?logo=windows&logoColor=white)
![Database](https://img.shields.io/badge/Database-SQLite3%20WAL-003B57.svg?logo=sqlite&logoColor=white)
![Binary](https://img.shields.io/badge/Tama%C3%B1o%20Exe-13%20MB%20(Single--File)-brightgreen.svg)
![QA Tests](https://img.shields.io/badge/Tests-28%20Aprobados%20(100%25)-success.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

> **Sistema clínico y comercial autónomo de alto rendimiento para la emisión secuencial, previsualización en vivo, auditoría local e impresión física de recetas médico-veterinarias conforme a la normativa oficial de Agrocalidad (Ecuador).**

---

## ⚡ Descarga Rápida (Para Usuarios Finales)

No es necesario instalar .NET, dependencias ni compilar código. El sistema se distribuye como un único ejecutable autónomo (`Single-File`) compilado y empaquetado automáticamente por GitHub Actions.

👉 **[Descargar la última versión de Recetario_Agrocalidad.exe](https://github.com/Klopezxd/vet-prescription-generator/releases/latest)**

1. Descarga `Recetario_Agrocalidad.exe` (o el `.zip` correspondiente).
2. Haz doble clic en el archivo para iniciar la aplicación.
3. Se abrirá automáticamente una ventana optimizada sin barras de navegador (`Edge --app`), lista para emitir recetas.

---

## 📋 Contexto Regulatorio y Propósito

En Ecuador, la **Agencia de Regulación y Control Fito y Zoosanitario (Agrocalidad)** exige que el expendio de medicamentos veterinarios bajo prescripción (antibióticos, biológicos, hormonales, anestésicos y sustancias controladas) se realice obligatoriamente mediante una **receta médico-veterinaria oficial**.

Esta plataforma digitaliza y acelera el flujo de trabajo en consultorios, distribuidoras de insumos agropecuarios y farmacias veterinarias:

1. **Formato Oficial de Dos Cuerpos en una Sola Hoja A4 Horizontal:**
   * **Cuerpo 1 (Original — Almacenista):** Datos del médico prescriptor (Cédula, N° Registro SENESCYT, Teléfono), secuencial oficial de control, fecha, diagnóstico presuntivo y prescripción farmacológica. Se archiva para fiscalización e inventario.
   * **Cuerpo 2 (Duplicado — Propietario del animal):** Datos del paciente, especie, propietario, dosificación, posología detallada e instrucciones de administración.
   * **Impresión Física Inmediata (<50 ms):** Ambos cuerpos se diagraman en una sola hoja A4 apaisada con línea de corte manual central y sellos, optimizados mediante reglas CSS Print nativas (`@page { size: A4 landscape; margin: 4mm; }`), eliminando desfases de formato y la lentitud de procesadores de texto externos.

2. **Numeración Secuencial Inalterable y Auditoría (SQLite):**
   * Correlativo atómico autoincremental (`0001`, `0002`, ...) protegido con transacciones ACID en modo WAL.
   * Módulos de edición, anulación oficial (con justificación auditada) y reactivación sin romper la secuencia correlativa.
   * Exportación y restauración en caliente de respaldos de la base de datos en 1 clic.

---

## 🏗️ Arquitectura Técnica (C# .NET 10 Nativo)

```mermaid
flowchart TD
    subgraph Frontend["Interfaz de Usuario (Modo App)"]
        UI["Panel Dividido Responsivo<br>(Formulario 440px + Vista Previa Dinámica con Zoom)"]
    end

    subgraph Backend["Capa Nativa C# .NET 10 (WinExe - ~13 MB)"]
        Server["Servidor HTTP Embebido (HttpListener en 127.0.0.1:8765)"]
        Launcher["Lanzador Shell (Microsoft Edge / Chrome en modo --app)"]
    end

    subgraph Storage["Persistencia Local ACID"]
        DB[(SQLite3 WAL: recetas.db)]
        Meta[(Configuración del Médico Veterinario en app_metadata)]
    end

    subgraph Output["Salida Física Oficial"]
        Print["Impresión Directa A4 Paisaje<br>(2 Cuerpos Simétricos / Sin hojas en blanco)"]
    end

    UI -->|Peticiones REST / JSON| Server
    Launcher -->|Abre Ventana Nativa Maximizada| UI
    Server <-->|Correlativo Atómico, CRUD y Auditoría| DB
    Server <-->|Datos Profesionales y Membrete| Meta
    UI -->|Diálogo de Impresión Nativo| Print
```

---

## 🌟 Características Principales

* ⚡ **Previsualización en Vivo con Zoom Interactivo:** Vista dividida (*Split View*) permanente. Mientras se digitan los campos, el documento oficial se renderiza en tiempo real idéntico al resultado impreso, con controles interactivos de escala (50% a 150%) y ajuste automático a pantalla completa.
* 🐾 **Chips de Especie Rápida:** Acceso inmediato con un clic para especies habituales: Bovino, Porcino, Equino, Canino, Felino, Ovino, Caprino y Aves.
* 🖨️ **Impresión Directa Calibrada a 1 Hoja:** Reglas CSS `@media print` de alta precisión que garantizan exactamente una página física en cualquier impresora, sin márgenes excesivos ni páginas sobrantes en blanco.
* ✏️ **Ciclo de Vida y Gestión Histórica de Recetas:**
  * **Editar:** Corrección de datos clínicos conservando el número correlativo original.
  * **Anular:** Marcado oficial como `ANULADA` con motivo de auditoría requerido por inspectores de Agrocalidad.
  * **Reactivar:** Restauración instantánea de recetas anuladas por error.
  * **Eliminar:** Limpieza de registros de prueba.
* 📦 **Copias de Seguridad en Caliente:** Respaldo completo de la base de datos (`.db`) descargable y módulo de restauración integral con validación de esquema.
* 👨‍⚕️ **Membrete Profesional Parametrizable:** Configuración persistente del nombre del médico veterinario, número de cédula, registro SENESCYT, teléfono y clínica.
* 🧪 **Calidad y Cobertura de Pruebas (QA):** Suite de **28 pruebas automatizadas** que validan integridad referencial, transacciones concurrentes con múltiples hilos (estrés SQLite), endpoints REST y reglas de negocio.

---

## 🔄 Automatización CI/CD y Generación de Versiones

El repositorio incluye dos flujos de trabajo en **GitHub Actions**:

1. **`lint.yml` (Calidad Continua):**
   * Se ejecuta en cada `push` o `pull_request` a la rama `main`.
   * Restaura, compila y ejecuta la suite de 28 pruebas automatizadas en un entorno Windows nativo.
2. **`release.yml` (Publicación Automática de Ejecutables):**
   * Se dispara automáticamente al crear un tag de versión (`v1.0.0`, `v1.1.0`, etc.) o manualmente desde la pestaña **Actions** con el botón **Run workflow**.
   * Compila el binario autónomo `Single-File Trimmed AOT` en .NET 10.
   * Genera el ejecutable `Recetario_Agrocalidad.exe` y el paquete `Recetario_Agrocalidad_win-x64.zip`.
   * Publica automáticamente una **GitHub Release** pública con las notas de versión y los archivos descargables listos para el usuario final.

### Cómo publicar una nueva versión:
```bash
# Método 1: Mediante etiquetas de Git
git tag v1.0.0
git push origin v1.0.0

# Método 2: Desde la web de GitHub
# Ve a Actions -> Release Executable -> Run workflow -> Ingresa el tag y ejecuta.
```

---

## 💻 Entorno de Desarrollo Local

### Requisitos:
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (o superior).
* Windows 10 u 11 (64 bits).

### Comandos de Desarrollo:
```powershell
# 1. Ejecutar en modo desarrollo
dotnet run

# 2. Ejecutar la suite de pruebas automatizadas (28 tests)
dotnet test tests/RecetarioAgrocalidad.Tests/RecetarioAgrocalidad.Tests.csproj

# 3. Compilar el ejecutable autónomo localmente
.\build_exe.bat
```

---

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo `LICENSE` para más detalles.
