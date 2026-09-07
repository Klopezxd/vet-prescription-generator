"""
Servidor API REST y renderizador web con FastAPI para el Generador de Recetas Veterinarias.
"""

from pathlib import Path
from typing import Any

from fastapi import FastAPI, HTTPException, Query, Request
from fastapi.responses import HTMLResponse
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
    return templates.TemplateResponse("index.html", {"request": request})


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


@app.get("/api/config")
async def get_configuration() -> dict[str, str]:
    """Obtiene los datos del médico veterinario prescriptor."""
    return db.get_vet_config()


@app.post("/api/config")
async def update_configuration(cfg: VetConfigUpdate) -> dict[str, str]:
    """Actualiza los datos del médico veterinario y establecimiento."""
    db.save_vet_config(cfg.model_dump())
    return {"status": "success", "message": "Configuración actualizada"}
