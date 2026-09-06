"""
Motor de generación de documentos DOCX y conversión a PDF.
Aplica principios de Clean Code, separación de responsabilidades y manejo de errores.
"""

import logging
import zipfile
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Dict, Optional, Tuple

from src.config import AppConfig

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

    def to_template_dict(self, recipe_number_str: str) -> Dict[str, str]:
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
            "NUM_RECETA": recipe_number_str
        }


@dataclass
class GenerationResult:
    """Resultado de la emisión de una receta."""
    recipe_number: str
    docx_path: Path
    pdf_path: Optional[Path] = None
    pdf_converted: bool = False
    warning_message: Optional[str] = None


class PrescriptionEngine:
    """Orquestador de lectura de plantilla, inyección de variables y exportación."""

    def __init__(self, config: Optional[AppConfig] = None) -> None:
        self.config = config or AppConfig()

    def ensure_directories(self) -> None:
        """Garantiza la existencia de las carpetas de salida."""
        self.config.word_dir_path.mkdir(parents=True, exist_ok=True)
        self.config.pdf_dir_path.mkdir(parents=True, exist_ok=True)

    def get_and_increment_counter(self) -> int:
        """Obtiene el número de receta actual e incrementa el contador atómicamente."""
        counter_file = self.config.counter_path
        current_number = 1

        if counter_file.exists():
            try:
                content = counter_file.read_text(encoding="utf-8").strip()
                if content.isdigit():
                    current_number = int(content) + 1
            except Exception as e:
                logger.warning("No se pudo leer el contador, reiniciando en 1: %s", e)

        try:
            counter_file.write_text(str(current_number), encoding="utf-8")
        except Exception as e:
            logger.error("Error al escribir el contador: %s", e)

        return current_number

    def render_docx(self, template_path: Path, output_path: Path, replacements: Dict[str, str]) -> None:
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

    def convert_to_pdf(self, docx_path: Path, pdf_path: Path) -> Tuple[bool, Optional[str]]:
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
        """Flujo completo de emisión de receta."""
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

        return GenerationResult(
            recipe_number=recipe_str,
            docx_path=docx_path,
            pdf_path=pdf_path if pdf_success else None,
            pdf_converted=pdf_success,
            warning_message=warning
        )
