#!/usr/bin/env python3
"""
Punto de entrada principal para el Generador de Recetas Veterinarias.
"""

import sys
import tkinter as tk

if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except AttributeError:
        pass

from src.gui import PrescriptionGUI


def main() -> None:
    """Inicializa la ventana principal de la aplicación."""
    root = tk.Tk()
    _app = PrescriptionGUI(root)
    root.mainloop()


if __name__ == "__main__":
    main()
