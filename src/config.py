"""
Configuración centralizada para el Generador de Recetas Veterinarias.
Aplica principios de Clean Code con rutas dinámicas y soporte para entornos empaquetados (PyInstaller).
"""

import sys
from dataclasses import dataclass, field
from pathlib import Path


def _resolve_base_dir() -> Path:
    """
    Ruta base para los datos de usuario persistentes (BD, recetas generadas).
    Si se ejecuta como ejecutable congelado (PyInstaller), apunta al directorio del .exe.
    En desarrollo, apunta a la raíz del repositorio.
    """
    if getattr(sys, "frozen", False):
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent.parent


def _resolve_bundle_dir() -> Path:
    """
    Ruta base para los recursos empaquetados de solo lectura (assets, plantillas base).
    En PyInstaller apunta al directorio temporal _MEIPASS.
    """
    if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
        return Path(sys._MEIPASS).resolve()
    return Path(__file__).resolve().parent.parent


@dataclass(frozen=True)
class AppConfig:
    """Configuración inmutable de rutas y persistencia del sistema."""
    base_dir: Path = field(default_factory=_resolve_base_dir)
    bundle_dir: Path = field(default_factory=_resolve_bundle_dir)
    db_filename: str = "recetas.db"

    @property
    def db_path(self) -> Path:
        return self.base_dir / self.db_filename


