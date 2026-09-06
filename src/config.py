"""
Configuración centralizada para el Generador de Recetas Veterinarias.
Aplica principios de Clean Code con rutas dinámicas y valores por defecto.
"""

from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class AppConfig:
    """Configuración inmutable de rutas y archivos del sistema."""
    base_dir: Path = Path(__file__).resolve().parent.parent
    template_filename: str = "plantilla_receta.docx"
    example_template_filename: str = "assets/plantilla_ejemplo.docx"
    counter_filename: str = "contador_recetas.txt"
    word_output_dir: str = "Recetas_Word"
    pdf_output_dir: str = "Recetas_PDF"

    @property
    def template_path(self) -> Path:
        """
        Retorna la ruta de la plantilla a utilizar.
        Prioriza la plantilla local ('plantilla_receta.docx'); si no existe,
        utiliza la plantilla de ejemplo de 'assets/plantilla_ejemplo.docx'.
        """
        local_template = self.base_dir / self.template_filename
        if local_template.exists():
            return local_template
        return self.base_dir / self.example_template_filename

    @property
    def counter_path(self) -> Path:
        return self.base_dir / self.counter_filename

    @property
    def word_dir_path(self) -> Path:
        return self.base_dir / self.word_output_dir

    @property
    def pdf_dir_path(self) -> Path:
        return self.base_dir / self.pdf_output_dir
