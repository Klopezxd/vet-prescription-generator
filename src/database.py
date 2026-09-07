"""
Gestor de persistencia SQLite para el Generador de Recetas Veterinarias.
Proporciona auditoría local, numeración secuencial atómica y búsquedas rápidas.
"""

import logging
import sqlite3
from pathlib import Path
from typing import Any

logger = logging.getLogger("recipe_database")


class DatabaseManager:
    """Gestiona la conexión y operaciones en la base de datos SQLite."""

    def __init__(self, db_path: Path, counter_file_path: Path | None = None) -> None:
        self.db_path = db_path
        self.counter_file_path = counter_file_path
        self._init_db()

    def _get_connection(self) -> sqlite3.Connection:
        """Crea y retorna una conexión configurada con soporte para nombres de columna."""
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        return conn

    def _init_db(self) -> None:
        """Inicializa las tablas y realiza migración del archivo contador plano si es necesario."""
        with self._get_connection() as conn:
            cursor = conn.cursor()

            # Tabla de metadatos del sistema (clave-valor)
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS app_metadata (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                );
            """)

            # Tabla principal de recetas emitidas
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS recetas (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    numero_receta TEXT UNIQUE NOT NULL,
                    numero_entero INTEGER NOT NULL,
                    fecha_emision TEXT NOT NULL,
                    dia TEXT,
                    mes TEXT,
                    anio TEXT,
                    especie TEXT NOT NULL,
                    nombre_paciente TEXT,
                    sexo TEXT,
                    edad TEXT,
                    nombre_propietario TEXT NOT NULL,
                    direccion_propietario TEXT,
                    prescripcion TEXT NOT NULL,
                    diagnostico TEXT,
                    posologia TEXT NOT NULL,
                    instrucciones TEXT,
                    ruta_docx TEXT,
                    ruta_pdf TEXT,
                    estado TEXT DEFAULT 'EMITIDA',
                    motivo_anulacion TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
            """)

            cursor.execute("CREATE INDEX IF NOT EXISTS idx_recetas_numero ON recetas(numero_receta);")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_recetas_propietario ON recetas(nombre_propietario);")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_recetas_fecha ON recetas(fecha_emision);")

            # Migración automática si la tabla ya existía previamente sin estas columnas
            try:
                cursor.execute("ALTER TABLE recetas ADD COLUMN estado TEXT DEFAULT 'EMITIDA';")
            except sqlite3.OperationalError:
                pass
            try:
                cursor.execute("ALTER TABLE recetas ADD COLUMN motivo_anulacion TEXT;")
            except sqlite3.OperationalError:
                pass


            # Migración inicial desde archivo contador_recetas.txt (si existe historial previo)
            if self.counter_file_path and self.counter_file_path.exists():
                try:
                    raw = self.counter_file_path.read_text(encoding="utf-8").strip()
                    if raw.isdigit():
                        file_counter = int(raw)
                        cursor.execute("SELECT value FROM app_metadata WHERE key = 'last_counter'")
                        row = cursor.fetchone()
                        if not row:
                            cursor.execute(
                                "INSERT INTO app_metadata (key, value) VALUES ('last_counter', ?)",
                                (str(file_counter),)
                            )
                except Exception as e:
                    logger.warning("No se pudo migrar el contador plano: %s", e)

            conn.commit()

    def get_next_recipe_number(self) -> int:
        """Calcula de forma atómica el siguiente número de receta correlativo."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(numero_entero) as max_num FROM recetas")
            row = cursor.fetchone()
            max_from_db = row["max_num"] if row and row["max_num"] is not None else 0

            cursor.execute("SELECT value FROM app_metadata WHERE key = 'last_counter'")
            meta_row = cursor.fetchone()
            meta_val = int(meta_row["value"]) if meta_row and meta_row["value"].isdigit() else 0

            return max(max_from_db, meta_val) + 1

    def save_prescription(
        self,
        numero_entero: int,
        numero_receta: str,
        fecha_emision: str,
        dia: str,
        mes: str,
        anio: str,
        especie: str,
        nombre_paciente: str,
        sexo: str,
        edad: str,
        nombre_propietario: str,
        direccion_propietario: str,
        prescripcion: str,
        diagnostico: str,
        posologia: str,
        instrucciones: str,
        ruta_docx: str,
        ruta_pdf: str | None = None
    ) -> int:
        """Guarda la receta emitida en la base de datos y actualiza el contador."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                INSERT INTO recetas (
                    numero_receta, numero_entero, fecha_emision, dia, mes, anio,
                    especie, nombre_paciente, sexo, edad,
                    nombre_propietario, direccion_propietario,
                    prescripcion, diagnostico, posologia, instrucciones,
                    ruta_docx, ruta_pdf
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                (
                    numero_receta,
                    numero_entero,
                    fecha_emision,
                    dia,
                    mes,
                    anio,
                    especie,
                    nombre_paciente,
                    sexo,
                    edad,
                    nombre_propietario,
                    direccion_propietario,
                    prescripcion,
                    diagnostico,
                    posologia,
                    instrucciones,
                    ruta_docx,
                    ruta_pdf
                )
            )

            # Actualizar metadato
            cursor.execute(
                """
                INSERT INTO app_metadata (key, value) VALUES ('last_counter', ?)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value
                """,
                (str(numero_entero),)
            )
            conn.commit()

            # Respaldo en archivo contador_recetas.txt para interoperabilidad
            if self.counter_file_path:
                try:
                    self.counter_file_path.write_text(str(numero_entero), encoding="utf-8")
                except Exception as e:
                    logger.warning("No se pudo actualizar el archivo de texto del contador: %s", e)

            return cursor.lastrowid or 0

    def search_prescriptions(self, term: str, limit: int = 50) -> list[dict[str, Any]]:
        """Busca recetas por número, propietario, paciente o diagnóstico."""
        like_term = f"%{term.strip()}%"
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                SELECT * FROM recetas
                WHERE numero_receta LIKE ?
                   OR nombre_propietario LIKE ?
                   OR nombre_paciente LIKE ?
                   OR prescripcion LIKE ?
                   OR diagnostico LIKE ?
                ORDER BY numero_entero DESC
                LIMIT ?
                """,
                (like_term, like_term, like_term, like_term, like_term, limit)
            )
            return [dict(row) for row in cursor.fetchall()]

    def get_recent_prescriptions(self, limit: int = 50) -> list[dict[str, Any]]:
        """Retorna las recetas emitidas más recientes."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT * FROM recetas ORDER BY numero_entero DESC LIMIT ?",
                (limit,)
            )
            return [dict(row) for row in cursor.fetchall()]

    def get_prescription_by_id(self, recipe_id: int) -> dict[str, Any] | None:
        """Obtiene una receta por su ID numérico primario."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM recetas WHERE id = ?", (recipe_id,))
            row = cursor.fetchone()
            return dict(row) if row else None

    def get_prescription_by_number(self, recipe_number: str) -> dict[str, Any] | None:
        """Obtiene una receta por su código secuencial formateado (ej: 0001)."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM recetas WHERE numero_receta = ?", (recipe_number,))
            row = cursor.fetchone()
            return dict(row) if row else None

    def get_vet_config(self) -> dict[str, str]:
        """Obtiene los datos guardados del médico veterinario y establecimiento."""
        keys = [
            "veterinario_nombre",
            "veterinario_cedula",
            "veterinario_senescyt",
            "veterinario_telefono",
            "establecimiento_nombre",
        ]
        result = {k: "" for k in keys}
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT key, value FROM app_metadata WHERE key LIKE 'veterinario_%' OR key = 'establecimiento_nombre'")
            for row in cursor.fetchall():
                result[row["key"]] = row["value"]
        return result

    def save_vet_config(self, config_dict: dict[str, str]) -> None:
        """Guarda o actualiza los datos del profesional prescriptor."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            for key, val in config_dict.items():
                cursor.execute(
                    """
                    INSERT INTO app_metadata (key, value) VALUES (?, ?)
                    ON CONFLICT(key) DO UPDATE SET value = excluded.value
                    """,
                    (key, str(val).strip())
                )
            conn.commit()

    def update_prescription(self, recipe_id: int, data: dict[str, Any]) -> bool:
        """Actualiza los datos de una receta existente conservando su número correlativo."""
        fecha_completa = f"{data.get('dia', '')}/{data.get('mes', '')}/{data.get('anio', '')}"
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                """
                UPDATE recetas
                SET dia = ?, mes = ?, anio = ?, fecha_emision = ?,
                    especie = ?, nombre_paciente = ?, sexo = ?, edad = ?,
                    nombre_propietario = ?, direccion_propietario = ?,
                    prescripcion = ?, diagnostico = ?, posologia = ?, instrucciones = ?
                WHERE id = ?
                """,
                (
                    str(data.get("dia", "")).strip(),
                    str(data.get("mes", "")).strip(),
                    str(data.get("anio", "")).strip(),
                    fecha_completa,
                    str(data.get("especie", "")).strip(),
                    str(data.get("nombre_paciente", "No aplica")).strip(),
                    str(data.get("sexo", "Macho")).strip(),
                    str(data.get("edad", "No especificada")).strip(),
                    str(data.get("nombre_propietario", "")).strip(),
                    str(data.get("direccion_propietario", "Particular")).strip(),
                    str(data.get("prescripcion", "")).strip(),
                    str(data.get("diagnostico", "Evaluación clínica")).strip(),
                    str(data.get("posologia", "")).strip(),
                    str(data.get("instrucciones", "Sin novedades")).strip(),
                    recipe_id,
                ),
            )
            conn.commit()
            return cursor.rowcount > 0

    def cancel_prescription(self, recipe_id: int, motivo: str = "Anulada por usuario") -> bool:
        """Marca una receta como ANULADA para control oficial de Agrocalidad sin generar saltos."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "UPDATE recetas SET estado = 'ANULADA', motivo_anulacion = ? WHERE id = ?",
                (motivo.strip(), recipe_id),
            )
            conn.commit()
            return cursor.rowcount > 0

    def delete_prescription(self, recipe_id: int) -> bool:
        """Elimina físicamente una receta de la base de datos."""
        with self._get_connection() as conn:
            cursor = conn.cursor()
            cursor.execute("DELETE FROM recetas WHERE id = ?", (recipe_id,))
            conn.commit()
            return cursor.rowcount > 0


