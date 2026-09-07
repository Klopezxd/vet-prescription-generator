using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using RecetarioAgrocalidad.Models;

namespace RecetarioAgrocalidad.Database;

public class DatabaseManager
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private static readonly byte[] SqliteHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");

    public DatabaseManager(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(AppContext.BaseDirectory, "recetas.db");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _connectionString = builder.ToString();
        InitDb();
    }

    public string DbPath => _dbPath;

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public void InitDb()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS app_metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

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

            CREATE INDEX IF NOT EXISTS idx_recetas_numero ON recetas(numero_receta);
            CREATE INDEX IF NOT EXISTS idx_recetas_propietario ON recetas(nombre_propietario);
            CREATE INDEX IF NOT EXISTS idx_recetas_fecha ON recetas(fecha_emision);
        ";
        cmd.ExecuteNonQuery();

        // Migraciones automáticas si la tabla existía previamente
        try
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE recetas ADD COLUMN estado TEXT DEFAULT 'EMITIDA';";
            alterCmd.ExecuteNonQuery();
        }
        catch { /* Columna ya existente */ }

        try
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE recetas ADD COLUMN motivo_anulacion TEXT;";
            alterCmd.ExecuteNonQuery();
        }
        catch { /* Columna ya existente */ }
    }

    public int GetNextRecipeNumber()
    {
        using var conn = CreateConnection();
        int maxFromDb = 0;
        int metaVal = 0;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT MAX(numero_entero) FROM recetas;";
            var result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                maxFromDb = Convert.ToInt32(result);
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT value FROM app_metadata WHERE key = 'last_counter';";
            var result = cmd.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int val))
            {
                metaVal = val;
            }
        }

        return Math.Max(maxFromDb, metaVal) + 1;
    }

    public (int id, string numeroReceta) SavePrescription(PrescriptionInput input)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        int nextNum = GetNextRecipeNumber();
        string numeroReceta = nextNum.ToString("D4");
        string fechaCompleta = $"{input.Dia}/{input.Mes}/{input.Anio}";

        long newId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO recetas (
                    numero_receta, numero_entero, fecha_emision, dia, mes, anio,
                    especie, nombre_paciente, sexo, edad,
                    nombre_propietario, direccion_propietario,
                    prescripcion, diagnostico, posologia, instrucciones,
                    ruta_docx, ruta_pdf, estado
                ) VALUES (
                    @numero_receta, @numero_entero, @fecha_emision, @dia, @mes, @anio,
                    @especie, @nombre_paciente, @sexo, @edad,
                    @nombre_propietario, @direccion_propietario,
                    @prescripcion, @diagnostico, @posologia, @instrucciones,
                    '', '', 'EMITIDA'
                );
                SELECT last_insert_rowid();
            ";

            cmd.Parameters.AddWithValue("@numero_receta", numeroReceta);
            cmd.Parameters.AddWithValue("@numero_entero", nextNum);
            cmd.Parameters.AddWithValue("@fecha_emision", fechaCompleta);
            cmd.Parameters.AddWithValue("@dia", input.Dia);
            cmd.Parameters.AddWithValue("@mes", input.Mes);
            cmd.Parameters.AddWithValue("@anio", input.Anio);
            cmd.Parameters.AddWithValue("@especie", input.Especie);
            cmd.Parameters.AddWithValue("@nombre_paciente", (object?)input.NombrePaciente ?? "No aplica");
            cmd.Parameters.AddWithValue("@sexo", input.Sexo);
            cmd.Parameters.AddWithValue("@edad", (object?)input.Edad ?? "No especificada");
            cmd.Parameters.AddWithValue("@nombre_propietario", input.NombrePropietario);
            cmd.Parameters.AddWithValue("@direccion_propietario", (object?)input.DireccionPropietario ?? "Particular");
            cmd.Parameters.AddWithValue("@prescripcion", input.Prescripcion);
            cmd.Parameters.AddWithValue("@diagnostico", (object?)input.Diagnostico ?? "Evaluación clínica");
            cmd.Parameters.AddWithValue("@posologia", input.Posologia);
            cmd.Parameters.AddWithValue("@instrucciones", (object?)input.Instrucciones ?? "Sin novedades");

            newId = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        // Actualizar last_counter en metadatos
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO app_metadata (key, value) VALUES ('last_counter', @val)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            ";
            cmd.Parameters.AddWithValue("@val", nextNum.ToString());
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return ((int)newId, numeroReceta);
    }

    public bool UpdatePrescription(int id, PrescriptionInput input)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();

        string fechaCompleta = $"{input.Dia}/{input.Mes}/{input.Anio}";
        cmd.CommandText = @"
            UPDATE recetas SET
                fecha_emision = @fecha_emision,
                dia = @dia,
                mes = @mes,
                anio = @anio,
                especie = @especie,
                nombre_paciente = @nombre_paciente,
                sexo = @sexo,
                edad = @edad,
                nombre_propietario = @nombre_propietario,
                direccion_propietario = @direccion_propietario,
                prescripcion = @prescripcion,
                diagnostico = @diagnostico,
                posologia = @posologia,
                instrucciones = @instrucciones
            WHERE id = @id;
        ";

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@fecha_emision", fechaCompleta);
        cmd.Parameters.AddWithValue("@dia", input.Dia);
        cmd.Parameters.AddWithValue("@mes", input.Mes);
        cmd.Parameters.AddWithValue("@anio", input.Anio);
        cmd.Parameters.AddWithValue("@especie", input.Especie);
        cmd.Parameters.AddWithValue("@nombre_paciente", (object?)input.NombrePaciente ?? "No aplica");
        cmd.Parameters.AddWithValue("@sexo", input.Sexo);
        cmd.Parameters.AddWithValue("@edad", (object?)input.Edad ?? "No especificada");
        cmd.Parameters.AddWithValue("@nombre_propietario", input.NombrePropietario);
        cmd.Parameters.AddWithValue("@direccion_propietario", (object?)input.DireccionPropietario ?? "Particular");
        cmd.Parameters.AddWithValue("@prescripcion", input.Prescripcion);
        cmd.Parameters.AddWithValue("@diagnostico", (object?)input.Diagnostico ?? "Evaluación clínica");
        cmd.Parameters.AddWithValue("@posologia", input.Posologia);
        cmd.Parameters.AddWithValue("@instrucciones", (object?)input.Instrucciones ?? "Sin novedades");

        int rows = cmd.ExecuteNonQuery();
        return rows > 0;
    }

    public bool CancelPrescription(int id, string motivo)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE recetas 
            SET estado = 'ANULADA', motivo_anulacion = @motivo 
            WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@motivo", string.IsNullOrWhiteSpace(motivo) ? "Anulada por usuario" : motivo.Trim());
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeletePrescription(int id)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM recetas WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public List<Prescription> SearchPrescriptions(string term, int limit = 50)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM recetas 
            WHERE numero_receta LIKE @term 
               OR nombre_propietario LIKE @term 
               OR nombre_paciente LIKE @term 
               OR diagnostico LIKE @term
            ORDER BY numero_entero DESC 
            LIMIT @limit;
        ";
        cmd.Parameters.AddWithValue("@term", $"%{term.Trim()}%");
        cmd.Parameters.AddWithValue("@limit", limit);

        return ReadPrescriptions(cmd);
    }

    public List<Prescription> GetRecentPrescriptions(int limit = 100)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM recetas ORDER BY numero_entero DESC LIMIT @limit;";
        cmd.Parameters.AddWithValue("@limit", limit);

        return ReadPrescriptions(cmd);
    }

    public Prescription? GetPrescriptionById(int id)
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM recetas WHERE id = @id LIMIT 1;";
        cmd.Parameters.AddWithValue("@id", id);

        var list = ReadPrescriptions(cmd);
        return list.Count > 0 ? list[0] : null;
    }

    private static List<Prescription> ReadPrescriptions(SqliteCommand cmd)
    {
        var list = new List<Prescription>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Prescription
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                NumeroReceta = reader.GetString(reader.GetOrdinal("numero_receta")),
                NumeroEntero = reader.GetInt32(reader.GetOrdinal("numero_entero")),
                FechaEmision = reader.IsDBNull(reader.GetOrdinal("fecha_emision")) ? "" : reader.GetString(reader.GetOrdinal("fecha_emision")),
                Dia = reader.IsDBNull(reader.GetOrdinal("dia")) ? "" : reader.GetString(reader.GetOrdinal("dia")),
                Mes = reader.IsDBNull(reader.GetOrdinal("mes")) ? "" : reader.GetString(reader.GetOrdinal("mes")),
                Anio = reader.IsDBNull(reader.GetOrdinal("anio")) ? "" : reader.GetString(reader.GetOrdinal("anio")),
                Especie = reader.IsDBNull(reader.GetOrdinal("especie")) ? "" : reader.GetString(reader.GetOrdinal("especie")),
                NombrePaciente = reader.IsDBNull(reader.GetOrdinal("nombre_paciente")) ? "" : reader.GetString(reader.GetOrdinal("nombre_paciente")),
                Sexo = reader.IsDBNull(reader.GetOrdinal("sexo")) ? "Macho" : reader.GetString(reader.GetOrdinal("sexo")),
                Edad = reader.IsDBNull(reader.GetOrdinal("edad")) ? "" : reader.GetString(reader.GetOrdinal("edad")),
                NombrePropietario = reader.IsDBNull(reader.GetOrdinal("nombre_propietario")) ? "" : reader.GetString(reader.GetOrdinal("nombre_propietario")),
                DireccionPropietario = reader.IsDBNull(reader.GetOrdinal("direccion_propietario")) ? "" : reader.GetString(reader.GetOrdinal("direccion_propietario")),
                Prescripcion = reader.IsDBNull(reader.GetOrdinal("prescripcion")) ? "" : reader.GetString(reader.GetOrdinal("prescripcion")),
                Diagnostico = reader.IsDBNull(reader.GetOrdinal("diagnostico")) ? "" : reader.GetString(reader.GetOrdinal("diagnostico")),
                Posologia = reader.IsDBNull(reader.GetOrdinal("posologia")) ? "" : reader.GetString(reader.GetOrdinal("posologia")),
                Instrucciones = reader.IsDBNull(reader.GetOrdinal("instrucciones")) ? "" : reader.GetString(reader.GetOrdinal("instrucciones")),
                Estado = reader.IsDBNull(reader.GetOrdinal("estado")) ? "EMITIDA" : reader.GetString(reader.GetOrdinal("estado")),
                MotivoAnulacion = reader.IsDBNull(reader.GetOrdinal("motivo_anulacion")) ? null : reader.GetString(reader.GetOrdinal("motivo_anulacion")),
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : reader.GetString(reader.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    public DoctorConfig GetDoctorConfig()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT key, value FROM app_metadata WHERE key LIKE 'veterinario_%' OR key = 'establecimiento_nombre';";

        var config = new DoctorConfig();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string key = reader.GetString(0);
            string val = reader.GetString(1);
            switch (key)
            {
                case "veterinario_nombre": config.VeterinarioNombre = val; break;
                case "veterinario_cedula": config.VeterinarioCedula = val; break;
                case "veterinario_senescyt": config.VeterinarioSenescyt = val; break;
                case "veterinario_telefono": config.VeterinarioTelefono = val; break;
                case "establecimiento_nombre": config.EstablecimientoNombre = val; break;
            }
        }
        return config;
    }

    public void SaveDoctorConfig(DoctorConfig config)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        var dict = new Dictionary<string, string>
        {
            ["veterinario_nombre"] = config.VeterinarioNombre ?? "",
            ["veterinario_cedula"] = config.VeterinarioCedula ?? "",
            ["veterinario_senescyt"] = config.VeterinarioSenescyt ?? "",
            ["veterinario_telefono"] = config.VeterinarioTelefono ?? "",
            ["establecimiento_nombre"] = config.EstablecimientoNombre ?? ""
        };

        foreach (var (key, val) in dict)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO app_metadata (key, value) VALUES (@key, @val)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            ";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@val", val);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public byte[] GetDatabaseBytes()
    {
        if (!File.Exists(_dbPath))
        {
            InitDb();
        }
        using var fs = new FileStream(_dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return ms.ToArray();
    }

    public bool RestoreDatabase(byte[] fileBytes)
    {
        if (fileBytes.Length < 16)
        {
            throw new InvalidOperationException("El archivo es demasiado pequeño para ser una base de datos válida.");
        }

        for (int i = 0; i < 16; i++)
        {
            if (fileBytes[i] != SqliteHeader[i])
            {
                throw new InvalidOperationException("El archivo suministrado no es una base de datos SQLite válida (cabecera no coincide).");
            }
        }

        // Limpiar conexiones SQLite abiertas en memoria
        SqliteConnection.ClearAllPools();

        // Respaldar base actual como medida de seguridad
        if (File.Exists(_dbPath))
        {
            string backupSafetyPath = Path.Combine(Path.GetDirectoryName(_dbPath) ?? "", "recetas_pre_restore.db");
            using (var src = new FileStream(_dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dst = new FileStream(backupSafetyPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                src.CopyTo(dst);
            }
        }

        // Sobrescribir con la nueva base de datos
        using (var dst = new FileStream(_dbPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            dst.Write(fileBytes, 0, fileBytes.Length);
        }

        // Re-inicializar para verificar integridad
        InitDb();
        return true;
    }
}
