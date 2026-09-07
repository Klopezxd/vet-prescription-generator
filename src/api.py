"""
Servidor API REST y renderizador web con FastAPI para el Generador de Recetas Veterinarias.
"""

import sys
from pathlib import Path
from typing import Any

from fastapi import FastAPI, File, HTTPException, Query, Request, UploadFile
from fastapi.responses import FileResponse, HTMLResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from pydantic import BaseModel, Field

from src.config import AppConfig
from src.database import DatabaseManager

config = AppConfig()
db = DatabaseManager(config.db_path, config.counter_path)

app = FastAPI(
    title="Recetario Veterinario - Agrocalidad Ecuador",
    description="Sistema de emisión, impresión instantánea y auditoría de recetas médicas veterinarias.",
    version="2.0.0",
)

# Servir archivos estáticos y plantillas
if getattr(sys, "frozen", False) and hasattr(sys, "_MEIPASS"):
    bundle_base = Path(sys._MEIPASS)
    static_dir = bundle_base / "src" / "static"
    templates_dir = bundle_base / "src" / "templates"
else:
    static_dir = Path(__file__).resolve().parent / "static"
    templates_dir = Path(__file__).resolve().parent / "templates"

app.mount("/static", StaticFiles(directory=str(static_dir)), name="static")
templates = Jinja2Templates(directory=str(templates_dir))



class PrescriptionCreate(BaseModel):
    """Esquema de validación para la emisión de recetas."""
    dia: str = Field(..., min_length=1, max_length=2)
    mes: str = Field(..., min_length=1, max_length=2)
    anio: str = Field(..., min_length=4, max_length=4)
    especie: str = Field(..., min_length=2)
    nombre_paciente: str = "No aplica"
    sexo: str = "Macho"
    edad: str = "No especificada"
    nombre_propietario: str = Field(..., min_length=2)
    direccion_propietario: str = "Particular"
    prescripcion: str = Field(..., min_length=3)
    diagnostico: str = "Evaluación clínica"
    posologia: str = Field(..., min_length=3)
    instrucciones: str = "Sin novedades"


class CancelRequest(BaseModel):
    """Esquema para anular una receta médica."""
    motivo: str = "Anulada por usuario"


class VetConfigUpdate(BaseModel):
    """Esquema para guardar la configuración del médico veterinario."""
    veterinario_nombre: str = ""
    veterinario_cedula: str = ""
    veterinario_senescyt: str = ""
    veterinario_telefono: str = ""
    establecimiento_nombre: str = ""



@app.get("/", response_class=HTMLResponse)
async def index(request: Request) -> HTMLResponse:
    """Ruta principal que sirve la interfaz interactiva."""
    return templates.TemplateResponse(request=request, name="index.html")



@app.get("/api/next-number")
async def get_next_number() -> dict[str, Any]:
    """Retorna el siguiente correlativo secuencial."""
    next_num = db.get_next_recipe_number()
    return {
        "next_number": next_num,
        "next_number_formatted": f"{next_num:04d}",
    }


@app.post("/api/recetas")
async def create_prescription(data: PrescriptionCreate) -> dict[str, Any]:
    """Registra y emite una receta veterinaria en la base de datos."""
    recipe_num = db.get_next_recipe_number()
    recipe_str = f"{recipe_num:04d}"
    fecha_completa = f"{data.dia}/{data.mes}/{data.anio}"

    row_id = db.save_prescription(
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
        ruta_docx="",
        ruta_pdf="",
    )

    return {
        "id": row_id,
        "numero_receta": recipe_str,
        "status": "success",
        "message": f"Receta N° {recipe_str} emitida exitosamente",
    }


@app.get("/api/recetas")
async def list_prescriptions(q: str | None = Query(default=None)) -> list[dict[str, Any]]:
    """Consulta el libro de recetas para auditoría oficial."""
    if q and q.strip():
        return db.search_prescriptions(q.strip())
    return db.get_recent_prescriptions(limit=100)


@app.get("/api/recetas/{recipe_id}")
async def get_prescription(recipe_id: int) -> dict[str, Any]:
    """Obtiene el detalle completo de una receta por su ID para reimpresión."""
    receta = db.get_prescription_by_id(recipe_id)
    if not receta:
        raise HTTPException(status_code=404, detail="Receta no encontrada")
    return receta


@app.put("/api/recetas/{recipe_id}")
async def update_prescription(recipe_id: int, data: PrescriptionCreate) -> dict[str, str]:
    """Actualiza una receta existente conservando su número correlativo."""
    receta = db.get_prescription_by_id(recipe_id)
    if not receta:
        raise HTTPException(status_code=404, detail="Receta no encontrada")
    success = db.update_prescription(recipe_id, data.model_dump())
    if not success:
        raise HTTPException(status_code=500, detail="No se pudo actualizar la receta")
    return {"status": "success", "message": f"Receta N° {receta['numero_receta']} actualizada exitosamente"}


@app.post("/api/recetas/{recipe_id}/anular")
async def cancel_prescription(recipe_id: int, cancel_data: CancelRequest | None = None) -> dict[str, str]:
    """Marca una receta como ANULADA para auditoría oficial de Agrocalidad sin generar saltos."""
    receta = db.get_prescription_by_id(recipe_id)
    if not receta:
        raise HTTPException(status_code=404, detail="Receta no encontrada")
    motivo = cancel_data.motivo if cancel_data else "Anulada por usuario"
    db.cancel_prescription(recipe_id, motivo=motivo)
    return {"status": "success", "message": f"Receta N° {receta['numero_receta']} anulada correctamente"}


@app.delete("/api/recetas/{recipe_id}")
async def delete_prescription(recipe_id: int) -> dict[str, str]:
    """Elimina definitivamente una receta de la base de datos."""
    receta = db.get_prescription_by_id(recipe_id)
    if not receta:
        raise HTTPException(status_code=404, detail="Receta no encontrada")
    db.delete_prescription(recipe_id)
    return {"status": "success", "message": f"Receta N° {receta['numero_receta']} eliminada"}


@app.get("/api/config")
async def get_configuration() -> dict[str, str]:
    """Obtiene los datos del médico veterinario prescriptor."""
    return db.get_vet_config()


@app.post("/api/config")
async def update_configuration(cfg: VetConfigUpdate) -> dict[str, str]:
    """Actualiza los datos del médico veterinario y establecimiento."""
    db.save_vet_config(cfg.model_dump())
    return {"status": "success", "message": "Configuración actualizada"}


@app.get("/api/backup-db")
async def backup_database():
    """Descarga una copia de seguridad directa de la base de datos SQLite."""
    from datetime import date
    if config.db_path.exists():
        fecha = date.today().strftime("%Y_%m_%d")
        return FileResponse(
            path=str(config.db_path),
            filename=f"recetas_backup_{fecha}.db",
            media_type="application/x-sqlite3",
        )
    raise HTTPException(status_code=404, detail="Aún no existe base de datos de recetas")


@app.post("/api/restore-db")
async def restore_database(file: UploadFile = File(...)) -> dict[str, str]:
    """Restaura la base de datos SQLite a partir de un archivo .db respaldado previamente."""
    if not file.filename or not file.filename.lower().endswith((".db", ".sqlite", ".sqlite3")):
        raise HTTPException(status_code=400, detail="El archivo debe tener extensión .db o .sqlite")

    content = await file.read()
    if not content.startswith(b"SQLite format 3\000"):
        raise HTTPException(status_code=400, detail="El archivo subido no es una base de datos SQLite válida")

    # Respaldo preventivo
    if config.db_path.exists():
        backup_path = config.db_path.with_name("recetas_pre_restore.db")
        try:
            config.db_path.replace(backup_path)
        except Exception:
            pass

    config.db_path.write_bytes(content)
    return {"status": "success", "message": "Base de datos restaurada exitosamente"}


