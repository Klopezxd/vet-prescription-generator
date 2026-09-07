using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecetarioAgrocalidad.Database;
using RecetarioAgrocalidad.Models;

namespace RecetarioAgrocalidad.Tests;

[TestClass]
public class DatabaseTests
{
    private string _tempDbPath = string.Empty;
    private DatabaseManager _db = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"recetas_test_{Guid.NewGuid():N}.db");
        _db = new DatabaseManager(_tempDbPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
            string wal = _tempDbPath + "-wal";
            string shm = _tempDbPath + "-shm";
            if (File.Exists(wal)) File.Delete(wal);
            if (File.Exists(shm)) File.Delete(shm);
        }
        catch { }
    }

    [TestMethod]
    public void Test_InitDb_CreatesTablesAndIndexes()
    {
        Assert.IsTrue(File.Exists(_tempDbPath));
        var recent = _db.GetRecentPrescriptions(10);
        Assert.IsNotNull(recent);
        Assert.AreEqual(0, recent.Count);
    }

    [TestMethod]
    public void Test_GetNextRecipeNumber_Initial_Returns1()
    {
        int next = _db.GetNextRecipeNumber();
        Assert.AreEqual(1, next);
    }

    [TestMethod]
    public void Test_SavePrescription_SequentialNumbering_And_Formatting()
    {
        var input1 = new PrescriptionInput
        {
            Dia = "07",
            Mes = "09",
            Anio = "2026",
            Especie = "Bovino",
            NombrePaciente = "Vaca 42",
            Sexo = "Hembra",
            Edad = "3 años",
            NombrePropietario = "Hacienda El Rocío",
            Prescripcion = "Oxitetraciclina 200 mg/ml, Frasco 100 ml",
            Posologia = "1 ml/10 kg vía IM cada 48h por 3 dosis"
        };

        var (id1, num1) = _db.SavePrescription(input1);
        Assert.IsTrue(id1 > 0);
        Assert.AreEqual("0001", num1);

        var input2 = new PrescriptionInput
        {
            Dia = "07",
            Mes = "09",
            Anio = "2026",
            Especie = "Canino",
            NombrePaciente = "Max",
            Sexo = "Macho",
            Edad = "2 años",
            NombrePropietario = "Juan Pérez",
            Prescripcion = "Amoxicilina + Ácido Clavulánico 250 mg",
            Posologia = "1 comprimido cada 12 horas por 7 días"
        };

        var (id2, num2) = _db.SavePrescription(input2);
        Assert.IsTrue(id2 > id1);
        Assert.AreEqual("0002", num2);

        int next = _db.GetNextRecipeNumber();
        Assert.AreEqual(3, next);
    }

    [TestMethod]
    public void Test_SavePrescription_SpecialCharacters_AccentsAndQuotes()
    {
        var input = new PrescriptionInput
        {
            Dia = "01",
            Mes = "12",
            Anio = "2026",
            Especie = "Equino",
            NombrePaciente = "Azabache 'El Campeón' 🐴",
            Sexo = "Macho",
            Edad = "5 años",
            NombrePropietario = "María José Núñez & Cía. - Ñuñoa",
            Prescripcion = "Flunixin Meglumine 50 mg/ml (Inyectable), 50 ml",
            Diagnostico = "Cólico espasmódico agudo / Evaluación clínica",
            Posologia = "1.1 mg/kg IV cada 24 horas por 3 días.",
            Instrucciones = "Dieta blanda y reposo total."
        };

        var (id, num) = _db.SavePrescription(input);
        Assert.AreEqual("0001", num);

        var retrieved = _db.GetPrescriptionById(id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Azabache 'El Campeón' 🐴", retrieved.NombrePaciente);
        Assert.AreEqual("María José Núñez & Cía. - Ñuñoa", retrieved.NombrePropietario);
        Assert.AreEqual("Cólico espasmódico agudo / Evaluación clínica", retrieved.Diagnostico);
    }

    [TestMethod]
    public void Test_SearchPrescriptions_MatchesOwnerPatientOrDiagnosis()
    {
        _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "Rocky",
            NombrePropietario = "Carlos Mendoza",
            Prescripcion = "Cefalexina 500mg",
            Posologia = "1 tab c/12h",
            Diagnostico = "Dermatitis atópica"
        });

        _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Felino",
            NombrePaciente = "Mishi",
            NombrePropietario = "Ana Gómez",
            Prescripcion = "Meloxicam gotas",
            Posologia = "2 gotas c/24h",
            Diagnostico = "Gingivitis felina"
        });

        var results1 = _db.SearchPrescriptions("Carlos");
        Assert.AreEqual(1, results1.Count);
        Assert.AreEqual("Rocky", results1[0].NombrePaciente);

        var results2 = _db.SearchPrescriptions("Gingivitis");
        Assert.AreEqual(1, results2.Count);
        Assert.AreEqual("Ana Gómez", results2[0].NombrePropietario);

        var results3 = _db.SearchPrescriptions("0001");
        Assert.AreEqual(1, results3.Count);
    }

    [TestMethod]
    public void Test_UpdatePrescription_ModifiesExistingRecord()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Bovino",
            NombrePaciente = "Toro 01",
            NombrePropietario = "Finca El Paraíso",
            Prescripcion = "Ivermectina 1%",
            Posologia = "1 ml/50 kg SC"
        });

        var updateInput = new PrescriptionInput
        {
            Dia = "08",
            Mes = "09",
            Anio = "2026",
            Especie = "Bovino",
            NombrePaciente = "Toro 01 (Corregido)",
            Sexo = "Macho",
            Edad = "4 años",
            NombrePropietario = "Finca El Paraíso - Sector B",
            Prescripcion = "Ivermectina 3.15% Larga Acción",
            Posologia = "1 ml/50 kg SC dosis única"
        };

        bool ok = _db.UpdatePrescription(id, updateInput);
        Assert.IsTrue(ok);

        var updated = _db.GetPrescriptionById(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Toro 01 (Corregido)", updated.NombrePaciente);
        Assert.AreEqual("Finca El Paraíso - Sector B", updated.NombrePropietario);
        Assert.AreEqual("Ivermectina 3.15% Larga Acción", updated.Prescripcion);
    }

    [TestMethod]
    public void Test_CancelPrescription_MarksAnuladaWithAuditReason()
    {
        var (id, num) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "Toby",
            NombrePropietario = "Pedro Sánchez",
            Prescripcion = "Ketoprofeno 20mg",
            Posologia = "1 comprimido diario"
        });

        bool canceled = _db.CancelPrescription(id, "Error de dosificación en posología");
        Assert.IsTrue(canceled);

        var r = _db.GetPrescriptionById(id);
        Assert.IsNotNull(r);
        Assert.AreEqual("ANULADA", r.Estado);
        Assert.AreEqual("Error de dosificación en posología", r.MotivoAnulacion);
    }

    [TestMethod]
    public void Test_DeletePrescription_RemovesRecord()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "Prueba",
            NombrePropietario = "Test",
            Prescripcion = "Test",
            Posologia = "Test"
        });

        bool deleted = _db.DeletePrescription(id);
        Assert.IsTrue(deleted);

        var r = _db.GetPrescriptionById(id);
        Assert.IsNull(r);
    }

    [TestMethod]
    public void Test_DoctorConfig_SaveAndRetrieve()
    {
        var config = new DoctorConfig
        {
            VeterinarioNombre = "Dr. Carlos López M.",
            VeterinarioCedula = "1712345678",
            VeterinarioSenescyt = "1005-2018-123456",
            VeterinarioTelefono = "0991234567",
            EstablecimientoNombre = "Distribuidora Veterinaria Andina"
        };

        _db.SaveDoctorConfig(config);

        var loaded = _db.GetDoctorConfig();
        Assert.AreEqual("Dr. Carlos López M.", loaded.VeterinarioNombre);
        Assert.AreEqual("1712345678", loaded.VeterinarioCedula);
        Assert.AreEqual("1005-2018-123456", loaded.VeterinarioSenescyt);
        Assert.AreEqual("0991234567", loaded.VeterinarioTelefono);
        Assert.AreEqual("Distribuidora Veterinaria Andina", loaded.EstablecimientoNombre);
    }

    [TestMethod]
    public void Test_BackupAndRestore_IntegrityRoundtrip()
    {
        _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Bovino",
            NombrePaciente = "Vaca Sagrada",
            NombrePropietario = "Hacienda San Pablo",
            Prescripcion = "Complejo B 100ml",
            Posologia = "10 ml IM"
        });

        byte[] backupBytes = _db.GetDatabaseBytes();
        Assert.IsNotNull(backupBytes);
        Assert.IsTrue(backupBytes.Length > 1024);

        // Crear una base de datos nueva vacía
        string restoreDbPath = Path.Combine(Path.GetTempPath(), $"recetas_restored_{Guid.NewGuid():N}.db");
        try
        {
            var restoreDb = new DatabaseManager(restoreDbPath);
            Assert.AreEqual(0, restoreDb.GetRecentPrescriptions().Count);

            // Restaurar con los bytes de respaldo
            bool ok = restoreDb.RestoreDatabase(backupBytes);
            Assert.IsTrue(ok);

            var list = restoreDb.GetRecentPrescriptions();
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Vaca Sagrada", list[0].NombrePaciente);
            Assert.AreEqual("Hacienda San Pablo", list[0].NombrePropietario);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(restoreDbPath)) File.Delete(restoreDbPath);
            string wal = restoreDbPath + "-wal";
            string shm = restoreDbPath + "-shm";
            if (File.Exists(wal)) File.Delete(wal);
            if (File.Exists(shm)) File.Delete(shm);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Test_RestoreDatabase_RejectsCorruptedFile()
    {
        byte[] corruptedBytes = Encoding.UTF8.GetBytes("ESTO NO ES UNA BASE DE DATOS SQLITE3 VALIDA");
        _db.RestoreDatabase(corruptedBytes);
    }

    [TestMethod]
    public void Test_ModelValidation_EnforcesAgrocalidadRequiredFields()
    {
        var inputInvalid = new PrescriptionInput
        {
            NombrePropietario = "", // Vacío
            Especie = "Canino",
            Prescripcion = "Antibiótico",
            Posologia = "1 tab c/12h"
        };

        bool valid = inputInvalid.Validate(out string? error);
        Assert.IsFalse(valid);
        Assert.IsTrue(error?.Contains("propietario") ?? false);

        var inputValid = new PrescriptionInput
        {
            NombrePropietario = "Juan Pérez",
            Especie = "Canino",
            Prescripcion = "Amoxicilina 500mg",
            Posologia = "1 tableta cada 12 horas por 7 días"
        };

        valid = inputValid.Validate(out error);
        Assert.IsTrue(valid);
        Assert.IsNull(error);
        Assert.AreEqual(DateTime.Now.ToString("dd"), inputValid.Dia);
    }

    [TestMethod]
    public void Test_ReactivatePrescription_RestoresEmitidaState()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Bovino",
            NombrePaciente = "Toro 99",
            NombrePropietario = "Finca La Esperanza",
            Prescripcion = "Antibiótico",
            Posologia = "Dosis"
        });

        // 1. Anular
        _db.CancelPrescription(id, "Anulado por error de digitación");
        var anulada = _db.GetPrescriptionById(id);
        Assert.IsNotNull(anulada);
        Assert.AreEqual("ANULADA", anulada.Estado);
        Assert.AreEqual("Anulado por error de digitación", anulada.MotivoAnulacion);

        // 2. Reactivar (deshacer anulación accidental)
        bool ok = _db.ReactivatePrescription(id);
        Assert.IsTrue(ok);

        var reactivada = _db.GetPrescriptionById(id);
        Assert.IsNotNull(reactivada);
        Assert.AreEqual("EMITIDA", reactivada.Estado);
        Assert.IsNull(reactivada.MotivoAnulacion);
    }
}
