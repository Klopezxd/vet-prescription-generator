"""
Motor de generación de documentos DOCX y conversión a PDF.
Aplica principios de Clean Code, separación de responsabilidades y manejo de errores.
"""

import logging
import zipfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from src.config import AppConfig
from src.database import DatabaseManager

logger = logging.getLogger("recipe_generator")


class TemplateNotFoundError(FileNotFoundError):
    """Lanzada cuando no se encuentra ninguna plantilla DOCX válida."""
    pass


@dataclass
class PrescriptionData:
    """Modelo de datos con la información requerida para emitir una receta."""
    dia: str
    mes: str
    anio: str
    especie: str
    nombre_paciente: str
    sexo: str
    edad: str
    nombre_propietario: str
    direccion_propietario: str
    prescripcion: str
    diagnostico: str
    posologia: str
    instrucciones: str

    def to_template_dict(self, recipe_number_str: str) -> dict[str, str]:
        """Convierte los datos al diccionario de sustitución de placeholders."""
        return {
            "DIA": self.dia.strip(),
            "MES": self.mes.strip(),
            "ANIO": self.anio.strip(),
            "ESPECIE": self.especie.strip(),
            "NOMBRE_PACIENTE": self.nombre_paciente.strip(),
            "SEXO": self.sexo.strip(),
            "EDAD": self.edad.strip(),
            "NOMBRE_PROPIETARIO": self.nombre_propietario.strip(),
            "DIRECCION_PROPIETARIO": self.direccion_propietario.strip(),
            "PRESCRIPCION": self.prescripcion.strip(),
            "DIAGNOSTICO": self.diagnostico.strip(),
            "POSOLOGIA": self.posologia.strip(),
            "INSTRUCCIONES": self.instrucciones.strip(),
            "NUM_RECETA": recipe_number_str,
        }


@dataclass
class GenerationResult:
    """Resultado de la emisión de una receta."""
    recipe_number: str
    docx_path: Path
    pdf_path: Path | None = None
    pdf_converted: bool = False
    warning_message: str | None = None


class PrescriptionEngine:
    """Orquestador de lectura de plantilla, inyección de variables, persistencia y exportación."""

    def __init__(self, config: AppConfig | None = None) -> None:
        self.config = config or AppConfig()
        self.db = DatabaseManager(self.config.db_path, self.config.counter_path)

    def ensure_directories(self) -> None:
        """Garantiza la existencia de las carpetas de salida."""
        self.config.word_dir_path.mkdir(parents=True, exist_ok=True)
        self.config.pdf_dir_path.mkdir(parents=True, exist_ok=True)

    def get_and_increment_counter(self) -> int:
        """Obtiene el número de receta actual e incrementa el contador atómicamente vía SQLite."""
        return self.db.get_next_recipe_number()

    def render_docx(self, template_path: Path, output_path: Path, replacements: dict[str, str]) -> None:
        """Lee la plantilla DOCX e inyecta las variables reemplazando los placeholders en el XML."""
        with zipfile.ZipFile(template_path, "r") as zin:
            with zipfile.ZipFile(output_path, "w", compression=zipfile.ZIP_DEFLATED) as zout:
                for item in zin.infolist():
                    data = zin.read(item.filename)
                    if item.filename == "word/document.xml":
                        xml = data.decode("utf-8")
                        for key, value in replacements.items():
                            xml = xml.replace(f"{{{{{key}}}}}", value)
                        data = xml.encode("utf-8")
                    zout.writestr(item, data)

    def convert_to_pdf(self, docx_path: Path, pdf_path: Path) -> tuple[bool, str | None]:
        """Convierte el documento DOCX a PDF usando docx2pdf (requiere Microsoft Word en Windows)."""
        try:
            from docx2pdf import convert
            convert(str(docx_path), str(pdf_path))
            return True, None
        except Exception as e:
            error_msg = f"No se pudo generar el PDF automáticamente: {e}"
            logger.warning(error_msg)
            return False, error_msg

    def generate(self, data: PrescriptionData) -> GenerationResult:
        """Flujo completo de emisión de receta y persistencia en base de datos."""
        template_path = self.config.template_path
        if not template_path.exists():
            raise TemplateNotFoundError(f"No se encontró la plantilla en: {template_path}")

        self.ensure_directories()

        recipe_num = self.get_and_increment_counter()
        recipe_str = f"{recipe_num:04d}"

        docx_path = self.config.word_dir_path / f"receta_{recipe_str}.docx"
        pdf_path = self.config.pdf_dir_path / f"receta_{recipe_str}.pdf"

        replacements = data.to_template_dict(recipe_str)
        self.render_docx(template_path, docx_path, replacements)

        pdf_success, warning = self.convert_to_pdf(docx_path, pdf_path)

        # Registro atómico en base de datos SQLite para auditoría
        fecha_completa = f"{data.dia}/{data.mes}/{data.anio}"
        self.db.save_prescription(
            numero_entero=recipe_num,
            numero_receta=recipe_str,
            fecha_emision=fecha_completa,
            dia=data.dia,
            mes=data.mes,
            anio=data.anio,
            especie=data.especie,
            nombre_paciente=data.nombre_paciente,
            sexo=data.sexo,
            edad=data.edad,
            nombre_propietario=data.nombre_propietario,
            direccion_propietario=data.direccion_propietario,
            prescripcion=data.prescripcion,
            diagnostico=data.diagnostico,
            posologia=data.posologia,
            instrucciones=data.instrucciones,
            ruta_docx=str(docx_path),
            ruta_pdf=str(pdf_path) if pdf_success else None,
        )

        return GenerationResult(
            recipe_number=recipe_str,
            docx_path=docx_path,
            pdf_path=pdf_path if pdf_success else None,
            pdf_converted=pdf_success,
            warning_message=warning,
        )

    def search_records(self, term: str, limit: int = 50) -> list[dict[str, Any]]:
        """Busca recetas históricas en la base de datos."""
        return self.db.search_prescriptions(term, limit)

    def get_recent_records(self, limit: int = 50) -> list[dict[str, Any]]:
        """Obtiene las recetas más recientes emitidas."""
        return self.db.get_recent_prescriptions(limit)
