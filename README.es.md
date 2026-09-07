# Generador de Recetas Veterinarias

![Python](https://img.shields.io/badge/Python-3.10%2B-3776AB.svg?logo=python&logoColor=white)
![UI](https://img.shields.io/badge/UI-Tkinter-blue.svg)
![Format](https://img.shields.io/badge/Export-DOCX%20%7C%20PDF-orange.svg)
![Linter](https://img.shields.io/badge/Linter-Ruff-000000.svg?logo=ruff&logoColor=white)
![License](https://img.shields.io/badge/License-PolyForm Noncommercial 1.0.0-green.svg)

> **Aplicación de escritorio para la automatización, emisión secuencial y exportación (Word y PDF) de recetas médicas veterinarias a partir de plantillas personalizables.**

*Read this document in English: [README.md](README.md)*

---

## Características

* **Emisión Ágil:** Rellena y genera recetas veterinarias completas en segundos mediante una interfaz gráfica intuitiva.
* **Autocompletado de Fecha:** Carga automáticamente el día, mes y año actual al abrir la aplicación, minimizando tareas repetitivas.
* **Numeración Secuencial:** Administra un contador incremental automático (`0001`, `0002`, etc.) persistido en archivo local.
* **Doble Exportación Automática:** Genera el documento editable en `.docx` y su respectiva versión final en `.pdf` (vía `docx2pdf`).
* **Seguridad y Privacidad por Diseño:**
  * Incluye una plantilla modelo sanitizada (`assets/plantilla_ejemplo.docx`).
  * Las plantillas reales con firmas o credenciales profesionales y las recetas emitidas están estrictamente protegidas mediante `.gitignore`.
* **Arquitectura Modular (Clean Code):** Desacoplamiento total entre el motor de inyección de datos (`PrescriptionEngine`) y la interfaz visual (`PrescriptionGUI`).

---

## Flujo de Procesamiento

```mermaid
flowchart LR
    A["Formulario Tkinter"] --> B["PrescriptionData"]
    B --> C["PrescriptionEngine"]
    D["Plantilla DOCX"] --> C
    E["Contador Secuencial"] --> C
    C --> F["Recetas_Word/receta_XXXX.docx"]
    F --> G["docx2pdf Converter"]
    G --> H["Recetas_PDF/receta_XXXX.pdf"]
```

---

## Requisitos del Sistema

* **Sistema Operativo:** Windows 10 u 11 (recomendado para la conversión nativa a PDF).
* **Python:** 3.10 o superior.
* **Microsoft Word:** Requerido en Windows para la conversión automática a PDF mediante la librería `docx2pdf`. (Si Word no está presente, la aplicación genera el archivo `.docx` sin interrumpir la ejecución).

---

## Instalación

```bash
# 1. Clonar el repositorio
git clone https://github.com/Klopezxd/vet-prescription-generator.git
cd vet-prescription-generator

# 2. Crear y activar entorno virtual (opcional pero recomendado)
python -m venv venv
.\venv\Scripts\activate

# 3. Instalar dependencias
pip install -r requirements.txt
```

---

## Uso

Ejecuta la interfaz gráfica:

```bash
python main.py
```

### Personalizar tu Plantilla:
1. El proyecto utiliza por defecto `assets/plantilla_ejemplo.docx`.
2. Para usar tu membrete profesional, coloca tu plantilla como `plantilla_receta.docx` en la raíz del proyecto.
3. El motor inyecta automáticamente los siguientes marcadores (*placeholders*):
   * `{{DIA}}`, `{{MES}}`, `{{ANIO}}`
   * `{{NUM_RECETA}}`
   * `{{ESPECIE}}`, `{{NOMBRE_PACIENTE}}`, `{{SEXO}}`, `{{EDAD}}`
   * `{{NOMBRE_PROPIETARIO}}`, `{{DIRECCION_PROPIETARIO}}`
   * `{{PRESCRIPCION}}`, `{{DIAGNOSTICO}}`, `{{POSOLOGIA}}`, `{{INSTRUCCIONES}}`

---

## Estructura del Repositorio

```text
vet-prescription-generator/
├── .github/
│   └── workflows/
│       └── lint.yml             # Integración continua con Ruff
├── assets/
│   └── plantilla_ejemplo.docx   # Plantilla sanitizada de demostración
├── src/
│   ├── __init__.py
│   ├── config.py                # Rutas del sistema y configuración
│   ├── generator.py             # Motor de renderizado XML y conversor PDF
│   └── gui.py                   # Interfaz de usuario en Tkinter
├── main.py                      # Punto de entrada de la aplicación
├── pyproject.toml               # Configuración del paquete y linter Ruff
├── requirements.txt             # Dependencias de Python
├── .gitignore                   # Excluye recetas emitidas y credenciales médicas
├── LICENSE                      # Licencia PolyForm Noncommercial 1.0.0
├── README.md                    # Documentación en inglés (predeterminada)
└── README.es.md                 # Documentación en español
```

---

## Licencia y Autoría

* **Autor:** [Klever López](https://github.com/Klopezxd)
* **Licencia:** Licencia PolyForm Noncommercial 1.0.0 — Código abierto para uso profesional, educativo y clínico.
