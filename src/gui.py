"""
Interfaz Gráfica de Usuario (GUI) en Tkinter para el Generador de Recetas Veterinarias.
Diseño limpio, validaciones de entrada y autocompletado inteligente de fecha.
"""

import tkinter as tk
from datetime import datetime
from tkinter import messagebox, scrolledtext
from typing import Optional

from src.config import AppConfig
from src.generator import (
    GenerationResult,
    PrescriptionData,
    PrescriptionEngine,
    TemplateNotFoundError,
)


class PrescriptionGUI:
    """Ventana principal para captura de datos y emisión de recetas."""

    def __init__(self, root: tk.Tk, engine: Optional[PrescriptionEngine] = None) -> None:
        self.root = root
        self.engine = engine or PrescriptionEngine()
        self.root.title("Generador de Recetas Veterinarias Pro")
        self.root.geometry("780x680")
        self.root.minsize(700, 600)

        self._build_ui()
        self._populate_default_date()

    def _build_ui(self) -> None:
        """Construye todos los componentes visuales de la interfaz."""
        main_frame = tk.Frame(self.root)
        main_frame.pack(fill="both", expand=True, padx=20, pady=15)

        # 1. Sección: Fecha
        frame_fecha = tk.LabelFrame(main_frame, text=" 📅 Fecha de Emisión ", padx=10, pady=8, font=("Arial", 10, "bold"))
        frame_fecha.grid(row=0, column=0, columnspan=2, sticky="ew", pady=(0, 10))

        tk.Label(frame_fecha, text="Día:").grid(row=0, column=0, sticky="e", padx=(0, 5))
        self.entry_dia = tk.Entry(frame_fecha, width=8)
        self.entry_dia.grid(row=0, column=1, sticky="w")

        tk.Label(frame_fecha, text="Mes:").grid(row=0, column=2, sticky="e", padx=(15, 5))
        self.entry_mes = tk.Entry(frame_fecha, width=8)
        self.entry_mes.grid(row=0, column=3, sticky="w")

        tk.Label(frame_fecha, text="Año:").grid(row=0, column=4, sticky="e", padx=(15, 5))
        self.entry_anio = tk.Entry(frame_fecha, width=10)
        self.entry_anio.grid(row=0, column=5, sticky="w")

        btn_hoy = tk.Button(frame_fecha, text="Hoy", command=self._populate_default_date, font=("Arial", 8), padx=6)
        btn_hoy.grid(row=0, column=6, padx=(15, 0))

        # 2. Sección: Datos del Paciente
        frame_paciente = tk.LabelFrame(main_frame, text=" 🐾 Datos del Paciente ", padx=10, pady=10, font=("Arial", 10, "bold"))
        frame_paciente.grid(row=1, column=0, sticky="nsew", pady=(0, 10), padx=(0, 8))

        tk.Label(frame_paciente, text="Especie (ej: Canino, Felino, Bovino):").grid(row=0, column=0, sticky="w")
        self.entry_especie = tk.Entry(frame_paciente, width=28)
        self.entry_especie.grid(row=1, column=0, sticky="ew", pady=(2, 8))

        tk.Label(frame_paciente, text="Nombre / Identificador del Paciente:").grid(row=2, column=0, sticky="w")
        self.entry_nombre_paciente = tk.Entry(frame_paciente, width=28)
        self.entry_nombre_paciente.grid(row=3, column=0, sticky="ew", pady=(2, 4))

        self.no_aplica_var = tk.BooleanVar(value=False)
        chk_no_aplica = tk.Checkbutton(
            frame_paciente,
            text="No aplica nombre",
            variable=self.no_aplica_var,
            command=self._toggle_no_aplica
        )
        chk_no_aplica.grid(row=4, column=0, sticky="w", pady=(0, 8))

        tk.Label(frame_paciente, text="Sexo:").grid(row=5, column=0, sticky="w")
        self.sexo_var = tk.StringVar(value="Macho")
        frame_sexo = tk.Frame(frame_paciente)
        frame_sexo.grid(row=6, column=0, sticky="w", pady=(2, 8))
        tk.Radiobutton(frame_sexo, text="Macho", variable=self.sexo_var, value="Macho").pack(side="left")
        tk.Radiobutton(frame_sexo, text="Hembra", variable=self.sexo_var, value="Hembra").pack(side="left", padx=(8, 0))
        tk.Radiobutton(frame_sexo, text="Indet.", variable=self.sexo_var, value="Indeterminado").pack(side="left", padx=(8, 0))

        tk.Label(frame_paciente, text="Edad (ej: 2 años, 6 meses):").grid(row=7, column=0, sticky="w")
        self.entry_edad = tk.Entry(frame_paciente, width=28)
        self.entry_edad.grid(row=8, column=0, sticky="ew", pady=(2, 0))

        # 3. Sección: Datos del Propietario
        frame_propietario = tk.LabelFrame(main_frame, text=" 👤 Datos del Propietario ", padx=10, pady=10, font=("Arial", 10, "bold"))
        frame_propietario.grid(row=1, column=1, sticky="nsew", pady=(0, 10), padx=(8, 0))

        tk.Label(frame_propietario, text="Nombres y Apellidos del Propietario:").grid(row=0, column=0, sticky="w")
        self.entry_nombre_prop = tk.Entry(frame_propietario, width=32)
        self.entry_nombre_prop.grid(row=1, column=0, sticky="ew", pady=(2, 10))

        tk.Label(frame_propietario, text="Dirección domiciliaria / Finca:").grid(row=2, column=0, sticky="w")
        self.entry_dir_prop = tk.Entry(frame_propietario, width=32)
        self.entry_dir_prop.grid(row=3, column=0, sticky="ew", pady=(2, 0))

        # 4. Sección: Información Médica y Prescripción
        frame_medica = tk.LabelFrame(main_frame, text=" 💊 Información Médica ", padx=10, pady=10, font=("Arial", 10, "bold"))
        frame_medica.grid(row=2, column=0, columnspan=2, sticky="ew", pady=(0, 10))

        tk.Label(frame_medica, text="Prescripción (Medicamento, concentración y forma):").grid(row=0, column=0, sticky="w")
        self.text_prescripcion = scrolledtext.ScrolledText(frame_medica, width=42, height=3)
        self.text_prescripcion.grid(row=1, column=0, sticky="ew", pady=(2, 8), padx=(0, 10))

        tk.Label(frame_medica, text="Diagnóstico Presuntivo / Clínico:").grid(row=0, column=1, sticky="w")
        self.text_diagnostico = scrolledtext.ScrolledText(frame_medica, width=42, height=3)
        self.text_diagnostico.grid(row=1, column=1, sticky="ew", pady=(2, 8))

        tk.Label(frame_medica, text="Posología (Dosis, frecuencia y duración):").grid(row=2, column=0, sticky="w")
        self.text_posologia = scrolledtext.ScrolledText(frame_medica, width=42, height=3)
        self.text_posologia.grid(row=3, column=0, sticky="ew", pady=(2, 4), padx=(0, 10))

        tk.Label(frame_medica, text="Instrucciones / Observaciones especiales:").grid(row=2, column=1, sticky="w")
        self.text_instrucciones = scrolledtext.ScrolledText(frame_medica, width=42, height=3)
        self.text_instrucciones.grid(row=3, column=1, sticky="ew", pady=(2, 4))

        btn_sin_novedades = tk.Button(
            frame_medica,
            text="Sin novedades",
            command=self._set_sin_novedades,
            bg="#2196F3",
            fg="white",
            font=("Arial", 8, "bold"),
            padx=8,
            pady=2
        )
        btn_sin_novedades.grid(row=4, column=1, sticky="w", pady=(0, 4))

        # 5. Botón de Acción Principal
        btn_generar = tk.Button(
            main_frame,
            text="📄 GENERAR RECETA (DOCX + PDF)",
            command=self._on_generar_click,
            bg="#2E7D32",
            fg="white",
            font=("Arial", 11, "bold"),
            padx=25,
            pady=8,
            relief="raised",
            cursor="hand2"
        )
        btn_generar.grid(row=3, column=0, columnspan=2, pady=12)

        # Ajuste responsivo de columnas
        main_frame.columnconfigure(0, weight=1)
        main_frame.columnconfigure(1, weight=1)
        frame_paciente.columnconfigure(0, weight=1)
        frame_propietario.columnconfigure(0, weight=1)
        frame_medica.columnconfigure(0, weight=1)
        frame_medica.columnconfigure(1, weight=1)

    def _populate_default_date(self) -> None:
        """Rellena automáticamente los campos de fecha con el día de hoy."""
        now = datetime.now()
        self.entry_dia.delete(0, tk.END)
        self.entry_dia.insert(0, f"{now.day:02d}")

        self.entry_mes.delete(0, tk.END)
        self.entry_mes.insert(0, f"{now.month:02d}")

        self.entry_anio.delete(0, tk.END)
        self.entry_anio.insert(0, str(now.year))

    def _toggle_no_aplica(self) -> None:
        """Activa o desactiva el campo de nombre del paciente."""
        if self.no_aplica_var.get():
            self.entry_nombre_paciente.delete(0, tk.END)
            self.entry_nombre_paciente.insert(0, "No aplica")
            self.entry_nombre_paciente.config(state="disabled")
        else:
            self.entry_nombre_paciente.config(state="normal")
            self.entry_nombre_paciente.delete(0, tk.END)

    def _set_sin_novedades(self) -> None:
        """Escribe 'Sin novedades' en las instrucciones."""
        self.text_instrucciones.delete("1.0", tk.END)
        self.text_instrucciones.insert("1.0", "Sin novedades")

    def _collect_data(self) -> Optional[PrescriptionData]:
        """Extrae y valida los datos de la interfaz."""
        dia = self.entry_dia.get().strip()
        mes = self.entry_mes.get().strip()
        anio = self.entry_anio.get().strip()
        especie = self.entry_especie.get().strip()

        if not (dia and mes and anio):
            messagebox.showerror("Dato requerido", "Por favor completa la fecha de emisión (día, mes y año).")
            return None

        if not especie:
            messagebox.showerror("Dato requerido", "Por favor indica la especie del paciente.")
            return None

        nombre_paciente = "No aplica" if self.no_aplica_var.get() else self.entry_nombre_paciente.get().strip()

        return PrescriptionData(
            dia=dia,
            mes=mes,
            anio=anio,
            especie=especie,
            nombre_paciente=nombre_paciente or "No aplica",
            sexo=self.sexo_var.get(),
            edad=self.entry_edad.get().strip() or "No especificada",
            nombre_propietario=self.entry_nombre_prop.get().strip() or "Particular",
            direccion_propietario=self.entry_dir_prop.get().strip() or "Sin dirección",
            prescripcion=self.text_prescripcion.get("1.0", tk.END).strip(),
            diagnostico=self.text_diagnostico.get("1.0", tk.END).strip(),
            posologia=self.text_posologia.get("1.0", tk.END).strip(),
            instrucciones=self.text_instrucciones.get("1.0", tk.END).strip()
        )

    def _on_generar_click(self) -> None:
        """Controlador del botón Generar Receta."""
        data = self._collect_data()
        if not data:
            return

        try:
            result = self.engine.generate(data)

            if result.pdf_converted:
                messagebox.showinfo(
                    "Receta Emitida con Éxito",
                    f"Receta N° {result.recipe_number} generada correctamente.\n\n"
                    f"📄 Word: {result.docx_path.name}\n"
                    f"📕 PDF: {result.pdf_path.name if result.pdf_path else 'N/A'}"
                )
            else:
                messagebox.showwarning(
                    "Word Generado con Advertencia",
                    f"Receta N° {result.recipe_number} creada en Word.\n"
                    f"No se pudo crear el PDF automáticamente (requiere MS Word instalado).\n"
                    f"Ruta: {result.docx_path}"
                )
        except TemplateNotFoundError as err:
            messagebox.showerror("Plantilla no encontrada", str(err))
        except Exception as err:
            messagebox.showerror("Error inesperado", f"Ocurrió un fallo al emitir la receta: {err}")
