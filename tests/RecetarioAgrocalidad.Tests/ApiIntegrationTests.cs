using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecetarioAgrocalidad.Database;
using RecetarioAgrocalidad.Models;
using RecetarioAgrocalidad.Server;

namespace RecetarioAgrocalidad.Tests;

[TestClass]
public class ApiIntegrationTests
{
    private static int _testPort = 8990;
    private int _port;
    private string _tempDbPath = string.Empty;
    private DatabaseManager _db = null!;
    private HttpServer _server = null!;
    private HttpClient _client = null!;
    private string _baseUrl = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _port = System.Threading.Interlocked.Increment(ref _testPort);
        _baseUrl = $"http://127.0.0.1:{_port}";
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"api_test_{Guid.NewGuid():N}.db");
        _db = new DatabaseManager(_tempDbPath);
        _server = new HttpServer(_db, _port);
        _server.Start();
        _client = new HttpClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            _client.Dispose();
            _server.Stop();
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
    public async Task Test_Heartbeat_ReturnsAlive()
    {
        var res = await _client.GetAsync($"{_baseUrl}/api/heartbeat");
        Assert.IsTrue(res.IsSuccessStatusCode);
        string json = await res.Content.ReadAsStringAsync();
        Assert.IsTrue(json.Contains("\"status\":\"alive\""));
    }

    [TestMethod]
    public async Task Test_GetNextNumber_ReturnsNextSequence()
    {
        var res = await _client.GetAsync($"{_baseUrl}/api/next-number");
        Assert.IsTrue(res.IsSuccessStatusCode);
        string json = await res.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<NextNumberResponse>(json);
        Assert.IsNotNull(data);
        Assert.AreEqual(1, data.NextNumber);
        Assert.AreEqual("0001", data.NextNumberFormatted);
    }

    [TestMethod]
    public async Task Test_VetConfig_GetAndPostRoundtrip()
    {
        var config = new DoctorConfig
        {
            VeterinarioNombre = "Dra. Valentina Paredes",
            VeterinarioCedula = "1799887766",
            VeterinarioSenescyt = "1002-2020-998877",
            VeterinarioTelefono = "0987654321",
            EstablecimientoNombre = "Clínica Veterinaria San Francisco"
        };

        var content = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json");
        var postRes = await _client.PostAsync($"{_baseUrl}/api/config", content);
        Assert.IsTrue(postRes.IsSuccessStatusCode);

        var getRes = await _client.GetAsync($"{_baseUrl}/api/config");
        Assert.IsTrue(getRes.IsSuccessStatusCode);
        string json = await getRes.Content.ReadAsStringAsync();
        var retrieved = JsonSerializer.Deserialize<DoctorConfig>(json);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Dra. Valentina Paredes", retrieved.VeterinarioNombre);
        Assert.AreEqual("Clínica Veterinaria San Francisco", retrieved.EstablecimientoNombre);
    }

    [TestMethod]
    public async Task Test_EmitirReceta_ValidPayload_Returns200AndIncrementsCounter()
    {
        var input = new PrescriptionInput
        {
            Dia = "07",
            Mes = "09",
            Anio = "2026",
            Especie = "Bovino",
            NombrePaciente = "Vaca Lola",
            Sexo = "Hembra",
            Edad = "3 años",
            NombrePropietario = "Finca Santa Inés",
            Prescripcion = "Ceftiofur 50 mg/ml, 100 ml",
            Posologia = "1 ml/50 kg IM cada 24h por 3 días",
            Diagnostico = "Metritis puerperal"
        };

        var content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, "application/json");
        var postRes = await _client.PostAsync($"{_baseUrl}/api/recetas", content);
        Assert.IsTrue(postRes.IsSuccessStatusCode);

        string resJson = await postRes.Content.ReadAsStringAsync();
        var recipeRes = JsonSerializer.Deserialize<CreateRecipeResponse>(resJson);
        Assert.IsNotNull(recipeRes);
        Assert.AreEqual("0001", recipeRes.NumeroReceta);
        Assert.AreEqual("success", recipeRes.Status);

        // Verificar que el siguiente número ahora sea 0002
        var nextRes = await _client.GetAsync($"{_baseUrl}/api/next-number");
        var nextData = JsonSerializer.Deserialize<NextNumberResponse>(await nextRes.Content.ReadAsStringAsync());
        Assert.IsNotNull(nextData);
        Assert.AreEqual("0002", nextData.NextNumberFormatted);
    }

    [TestMethod]
    public async Task Test_EmitirReceta_InvalidPayload_Returns400BadRequest()
    {
        // Enviar receta sin propietario ni prescripción
        var invalidInput = new PrescriptionInput
        {
            NombrePropietario = "",
            Prescripcion = "",
            Especie = "Canino",
            Posologia = ""
        };

        var content = new StringContent(JsonSerializer.Serialize(invalidInput), Encoding.UTF8, "application/json");
        var postRes = await _client.PostAsync($"{_baseUrl}/api/recetas", content);
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, postRes.StatusCode);

        string resJson = await postRes.Content.ReadAsStringAsync();
        Assert.IsTrue(resJson.Contains("\"status\":\"error\""));
    }

    [TestMethod]
    public async Task Test_GetPrescriptionById_Returns200AndAccuratePayload()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Felino",
            NombrePaciente = "Gaturro",
            NombrePropietario = "Lucía Mendez",
            Prescripcion = "Doxiciclina 50mg",
            Posologia = "1/2 tab cada 24h"
        });

        var res = await _client.GetAsync($"{_baseUrl}/api/recetas/{id}");
        Assert.IsTrue(res.IsSuccessStatusCode);

        var receta = JsonSerializer.Deserialize<Prescription>(await res.Content.ReadAsStringAsync());
        Assert.IsNotNull(receta);
        Assert.AreEqual("Gaturro", receta.NombrePaciente);
        Assert.AreEqual("Lucía Mendez", receta.NombrePropietario);
    }

    [TestMethod]
    public async Task Test_UpdatePrescription_Returns200()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "Bob",
            NombrePropietario = "Pedro Gómez",
            Prescripcion = "Tramadol",
            Posologia = "1 tab"
        });

        var updatePayload = new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "Bob (Actualizado)",
            NombrePropietario = "Pedro Gómez",
            Prescripcion = "Tramadol 50 mg",
            Posologia = "1 comprimido cada 8 horas por 3 días"
        };

        var content = new StringContent(JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");
        var putRes = await _client.PutAsync($"{_baseUrl}/api/recetas/{id}", content);
        Assert.IsTrue(putRes.IsSuccessStatusCode);

        var r = _db.GetPrescriptionById(id);
        Assert.IsNotNull(r);
        Assert.AreEqual("Bob (Actualizado)", r.NombrePaciente);
    }

    [TestMethod]
    public async Task Test_AnularPrescription_Returns200AndUpdatesState()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "Rex",
            NombrePropietario = "Ana",
            Prescripcion = "Rx",
            Posologia = "1 tab"
        });

        var content = new StringContent(JsonSerializer.Serialize(new AnularInput { Motivo = "Error de impresión" }), Encoding.UTF8, "application/json");
        var postRes = await _client.PostAsync($"{_baseUrl}/api/recetas/{id}/anular", content);
        Assert.IsTrue(postRes.IsSuccessStatusCode);

        var r = _db.GetPrescriptionById(id);
        Assert.IsNotNull(r);
        Assert.AreEqual("ANULADA", r.Estado);
        Assert.AreEqual("Error de impresión", r.MotivoAnulacion);
    }

    [TestMethod]
    public async Task Test_DeletePrescription_Returns200()
    {
        var (id, _) = _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Bovino",
            NombrePaciente = "Test",
            NombrePropietario = "Test",
            Prescripcion = "Test",
            Posologia = "Test"
        });

        var delRes = await _client.DeleteAsync($"{_baseUrl}/api/recetas/{id}");
        Assert.IsTrue(delRes.IsSuccessStatusCode);

        var r = _db.GetPrescriptionById(id);
        Assert.IsNull(r);
    }

    [TestMethod]
    public async Task Test_BackupEndpoint_DownloadsValidDatabaseBytes()
    {
        _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Equino",
            NombrePaciente = "Relámpago",
            NombrePropietario = "Establo La Pradera",
            Prescripcion = "Fenilbutazona 20%",
            Posologia = "10 ml IV"
        });

        var res = await _client.GetAsync($"{_baseUrl}/api/backup-db");
        Assert.IsTrue(res.IsSuccessStatusCode);
        byte[] bytes = await res.Content.ReadAsByteArrayAsync();
        Assert.IsTrue(bytes.Length > 100);
        string header = Encoding.ASCII.GetString(bytes, 0, 15);
        Assert.AreEqual("SQLite format 3", header);
    }

    [TestMethod]
    public async Task Test_RestoreEndpoint_ValidDatabase_RestoresSuccessfully()
    {
        _db.SavePrescription(new PrescriptionInput
        {
            Especie = "Canino",
            NombrePaciente = "PruebaRestore",
            NombrePropietario = "Propietario Original",
            Prescripcion = "Medicamento",
            Posologia = "Dosis"
        });

        byte[] backupBytes = _db.GetDatabaseBytes();

        // Borrar base actual
        _db.DeletePrescription(1);
        Assert.AreEqual(0, _db.GetRecentPrescriptions().Count);

        // Restaurar vía POST /api/restore-db
        var byteContent = new ByteArrayContent(backupBytes);
        byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        var restoreRes = await _client.PostAsync($"{_baseUrl}/api/restore-db", byteContent);
        Assert.IsTrue(restoreRes.IsSuccessStatusCode);

        var list = _db.GetRecentPrescriptions();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("PruebaRestore", list[0].NombrePaciente);
    }

    [TestMethod]
    public async Task Test_StaticFiles_ReturnsHtmlCssJs()
    {
        var htmlRes = await _client.GetAsync($"{_baseUrl}/");
        Assert.IsTrue(htmlRes.IsSuccessStatusCode);
        string html = await htmlRes.Content.ReadAsStringAsync();
        Assert.IsTrue(html.Contains("Agrocalidad"));

        var cssRes = await _client.GetAsync($"{_baseUrl}/static/css/app.css");
        Assert.IsTrue(cssRes.IsSuccessStatusCode);

        var jsRes = await _client.GetAsync($"{_baseUrl}/static/js/app.js");
        Assert.IsTrue(jsRes.IsSuccessStatusCode);
    }

    [TestMethod]
    public async Task Test_NonExistentRoute_Returns404()
    {
        var res = await _client.GetAsync($"{_baseUrl}/ruta-inexistente");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, res.StatusCode);
    }

    [TestMethod]
    public async Task Test_Concurrency_MultipleParallelRequests()
    {
        // Enviar 20 peticiones simultáneas de emisión y consulta
        var tasks = new Task<HttpResponseMessage>[20];
        for (int i = 0; i < 20; i++)
        {
            int index = i;
            tasks[i] = Task.Run(async () =>
            {
                if (index % 2 == 0)
                {
                    var payload = new PrescriptionInput
                    {
                        Especie = "Canino",
                        NombrePaciente = $"Paciente {index}",
                        NombrePropietario = $"Propietario {index}",
                        Prescripcion = "Amoxicilina 500mg",
                        Posologia = "1 tab c/12h"
                    };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    return await _client.PostAsync($"{_baseUrl}/api/recetas", content);
                }
                else
                {
                    return await _client.GetAsync($"{_baseUrl}/api/recetas");
                }
            });
        }

        var responses = await Task.WhenAll(tasks);
        foreach (var r in responses)
        {
            Assert.IsTrue(r.IsSuccessStatusCode);
        }

        // Verificar que todas las 10 recetas pares se hayan insertado sin duplicados
        var allRecetas = _db.GetRecentPrescriptions(100);
        Assert.AreEqual(10, allRecetas.Count);
    }
}
