# Generador de Recetas Veterinarias (Normativa Agrocalidad Ecuador)

![Python](https://img.shields.io/badge/Python-3.10%2B-3776AB.svg?logo=python&logoColor=white)
![FastAPI](https://img.shields.io/badge/Backend-FastAPI-009688.svg?logo=fastapi&logoColor=white)
![UI](https://img.shields.io/badge/UI-HTML5%20%7C%20CSS%20Print-E34F26.svg)
![Database](https://img.shields.io/badge/Database-SQLite3-003B57.svg?logo=sqlite&logoColor=white)
![Impresion](https://img.shields.io/badge/Impresi%C3%B3n-A4%20Horizontal%20(50ms)-brightgreen.svg)
![Linter](https://img.shields.io/badge/Linter-Ruff-000000.svg?logo=ruff&logoColor=white)
![License](https://img.shields.io/badge/License-PolyForm Noncommercial 1.0.0-green.svg)

> **Sistema clínico y comercial para la emisión secuencial, previsualización en vivo, auditoría local e impresión de recetas médico-veterinarias conforme a las exigencias regulatorias de Agrocalidad (Ecuador).**

---

## 📋 Contexto Regulatorio y Propósito

En Ecuador, la **Agencia de Regulación y Control Fito y Zoosanitario (Agrocalidad)** exige que el expendio de medicamentos de control especial (antibióticos, hormonales, biológicos, anestésicos y sustancias restringidas) se realice obligatoriamente mediante una **receta médico-veterinaria oficial**.

Esta plataforma fue diseñada para agilizar y digitalizar el flujo de trabajo en farmacias veterinarias, distribuidoras de insumos agropecuarios y consultorios clínicos, resolviendo dos problemas fundamentales:

1. **Formato Oficial de Dos Cuerpos en una Sola Hoja A4 Horizontal:**
   * **Cuerpo 1 (Original — Almacenista):** Datos del médico prescriptor (Cédula, N° Registro SENESCYT, Teléfono de contacto), número secuencial de control, fecha, diagnóstico presuntivo y prescripción farmacológica. Se archiva en el establecimiento para control de inventario e inspecciones de Agrocalidad.
   * **Cuerpo 2 (Duplicado — Propietario del animal):** Datos del paciente, especie, propietario, dosificación, posología detallada e indicaciones de administración.
   * **Impresión Inmediata (<50 ms):** Ambos cuerpos se diagraman en una sola hoja A4 en orientación apaisada con líneas de corte y sellos, optimizado mediante reglas de `@media print` y `@page` nativas del navegador, eliminando por completo la dependencia y demoras de Microsoft Word.

2. **Numeración Secuencial Inalterable y Auditoría (SQLite):**
   * Correlativo atómico autoincremental (`0001`, `0002`, ...) protegido a nivel de base de datos.
   * Modificación, anulación oficial y eliminación de registros históricos sin desfasar la numeración.
   * Descarga y restauración de copias de seguridad de la base de datos en 1 clic.

---

## 🏗️ Arquitectura del Sistema

```mermaid
flowchart TD
    subgraph Frontend["Interfaz de Usuario"]
        UI["Interfaz Web / Modo Escritorio<br>(HTML5 + CSS Print + JS Reactivo)"]
    end

    subgraph Backend["Capa de Negocio y Control"]
        API["FastAPI App (src/api.py)"]
        Desktop["Lanzador Nativo (desktop.py / main.py)"]
    end

    subgraph Storage["Persistencia y Auditoría"]
        DB[(SQLite3: recetas.db)]
        Meta[(Configuración del Veterinario)]
    end

    subgraph Output["Salida Vectorial"]
        Print["Impresión Directa A4 Horizontal<br>(Nativa del sistema / Cero dependencias)"]
    end

    UI -->|Peticiones REST / JSON| API
    Desktop -->|Inicia Servidor y App Mode| API
    API <-->|Correlativo, CRUD y Auditoría| DB
    API <-->|Datos del Profesional| Meta
    UI -->|Diálogo de Impresión Instantáneo| Print
```

---

## 🌟 Características Principales

* ⚡ **Previsualización en Tiempo Real (WYSIWYG):** Mientras se digita la información en el formulario, el documento oficial de dos cuerpos se actualiza instantáneamente en pantalla con el diseño exacto que saldrá de la impresora.
* 🐾 **Chips de Especie Rápida:** Botones de acceso directo con un clic para especies frecuentes: Bovino, Porcino, Equino, Canino, Felino, Ovino, Caprino y Aves.
* 🖨️ **Impresión Ultrarrápida sin Microsoft Word:** Diseñado con estándares CSS Print (`@page { size: A4 landscape; margin: 4mm; }`), imprime o guarda en PDF vectorial en menos de 50 milisegundos desde cualquier navegador.
* ✏️ **Módulo Completo de Gestión Histórica:**
  * **Editar:** Permite corregir errores de digitación en recetas pasadas conservando su número secuencial.
  * **Anular (Agrocalidad):** Marca la receta como anulada con motivo de auditoría sin dejar huecos en la numeración correlativa.
  * **Eliminar:** Borra registros de prueba.
* 📦 **Copias de Seguridad y Restauración:** Botones en la interfaz para descargar un respaldo completo de la base de datos (`.db`) o restaurar una copia previa al cambiar de equipo.
* 👨‍⚕️ **Perfil del Médico Veterinario Configurable:** Pestaña de ajustes para almacenar en SQLite el nombre del profesional, Cédula de Identidad, Registro SENESCYT, teléfono y nombre del establecimiento o clínica.
* 🔒 **Privacidad de Datos Clínicos:** La base de datos (`recetas.db`), backups y documentos emitidos están rigurosamente excluidos en `.gitignore`.

---

## 📋 Requisitos del Sistema

* **Para Desarrolladores:** Python 3.10 o superior.
* **Para el Usuario Final (con el `.exe`):** Windows 10 u 11 (no requiere tener Python instalado).

---

## 🚀 Instalación y Puesta en Marcha

### 1. Clonar el Repositorio e Instalar Dependencias

```powershell
# Clonar el proyecto
git clone https://github.com/Klopezxd/vet-prescription-generator.git
cd vet-prescription-generator

# Crear y activar entorno virtual
python -m venv venv
.\venv\Scripts\activate

# Instalar dependencias
pip install -r requirements.txt
```

### 2. Ejecutar la Aplicación

#### Opción A: Ejecutable Portable de Escritorio (Recomendado para Usuario Final)
* **100% Autónomo:** No requiere instalar Python en la máquina destino.
* Haz doble clic en el archivo generado localmente en:
  ```text
  dist/Recetario_Agrocalidad.exe
  ```
* *(Para regenerar el ejecutable tras cualquier cambio en el código, haz doble clic en **`build_exe.bat`**).*

#### Opción B: Inicio con Python (Modo Escritorio)
```powershell
python main.py
```
*O haz doble clic sobre el archivo **`iniciar_recetario.bat`**.*

#### Opción C: Inicio Manual por Terminal (Modo Servidor Web)
```powershell
python -m uvicorn src.api:app --reload --host 0.0.0.0 --port 8000
```
Luego abre en tu navegador: **`http://localhost:8000`**

---

## 📁 Estructura del Repositorio

```text
vet-prescription-generator/
├── .github/
│   └── workflows/
│       └── lint.yml             # Integración continua con Ruff
├── assets/
│   └── app_icon.ico             # Icono oficial veterinario multirresolución
├── src/
│   ├── __init__.py
│   ├── api.py                   # API REST y servidor web FastAPI
│   ├── config.py                # Rutas dinámicas y compatibilidad PyInstaller
│   ├── database.py              # Capa de persistencia SQLite y auditoría
│   ├── static/
│   │   ├── css/
│   │   │   └── app.css          # Estilos de pantalla y reglas @media print A4
│   │   └── js/
│   │       └── app.js           # Lógica interactiva, CRUD y comunicación con la API
│   └── templates/
│       └── index.html           # Vista principal con formulario y hoja oficial
├── build_exe.bat                # Script de compilación automática del ejecutable
├── desktop.py                   # Lanzador de escritorio nativo (FastAPI + App Mode)
├── iniciar_recetario.bat        # Lanzador web para Windows (1 clic)
├── main.py                      # Punto de entrada principal en Python
├── pyproject.toml               # Configuración del linter Ruff (PEP 8)
├── requirements.txt             # Dependencias del proyecto
├── .gitignore                   # Excluye recetas emitidas, binarios y base de datos
├── LICENSE                      # Licencia de código abierto PolyForm Noncommercial 1.0.0
└── README.md                    # Documentación técnica y funcional
```

---

## 🛡️ Calidad de Código y Estándares

El proyecto aplica estándares estrictos de desarrollo:
* **Linter y Formateador:** [Ruff](https://docs.astral.sh/ruff/) con 0 advertencias o errores de estilo.
* **Separación de Responsabilidades:** Capas independientes para UI, control de rutas, lógica de negocio y base de datos relacional.
* **Manejo Seguro de Conexiones:** Conexiones SQLite con context managers (`with`) y bloqueo transaccional para evitar condiciones de carrera en el número secuencial.

---

## 📄 Licencia y Créditos

* **Desarrollador:** [Klever López](https://github.com/Klopezxd)
* **Licencia:** Distribuido bajo la [Licencia PolyForm Noncommercial 1.0.0](LICENSE). Libre para uso comercial, agropecuario y adaptaciones clínicas.


