using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RecetarioAgrocalidad.Database;
using RecetarioAgrocalidad.Models;

namespace RecetarioAgrocalidad.Server;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly DatabaseManager _db;
    private readonly int _port;
    private bool _isRunning;
    private readonly string _webRoot;
    private long _lastHeartbeatTicks = DateTime.UtcNow.Ticks;
    private readonly DateTime _startupTime = DateTime.UtcNow;
    private readonly ManualResetEventSlim _shutdownSignal = new(false);

    public HttpServer(DatabaseManager db, int port = 8765)
    {
        _db = db;
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _webRoot = AppContext.BaseDirectory;
    }

    public void Start()
    {
        if (_isRunning) return;
        _listener.Start();
        _isRunning = true;
        Task.Run(ListenLoop);
    }

    public void WaitForShutdown()
    {
        while (!_shutdownSignal.Wait(TimeSpan.FromSeconds(2)))
        {
            var lastActivity = new DateTime(Interlocked.Read(ref _lastHeartbeatTicks), DateTimeKind.Utc);
            // Gracia inicial de 60s. Si transcurren más de 30s sin actividad ni latidos, la ventana se cerró
            if ((DateTime.UtcNow - _startupTime).TotalSeconds > 60 &&
                (DateTime.UtcNow - lastActivity).TotalSeconds > 30)
            {
                break;
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _shutdownSignal.Set();
        try
        {
            _listener.Stop();
        }
        catch { }
    }

    private async Task ListenLoop()
    {
        while (_isRunning)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (HttpListenerException) when (!_isRunning)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in listener loop: {ex.Message}");
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;
        Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);

        // Headers CORS
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 200;
            res.Close();
            return;
        }

        string path = req.Url?.AbsolutePath.TrimEnd('/') ?? "";
        if (string.IsNullOrEmpty(path)) path = "/";

        try
        {
            // 1. Archivos Estáticos y Plantilla Principal
            if (req.HttpMethod == "GET" && path == "/")
            {
                await ServeIndexHtml(res);
                return;
            }

            if (req.HttpMethod == "GET" && path.StartsWith("/static/", StringComparison.OrdinalIgnoreCase))
            {
                await ServeStaticFile(path, res);
                return;
            }

            // 2. API Endpoints
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                await HandleApiRoute(path, req, res);
                return;
            }

            // Ruta no encontrada
            res.StatusCode = 404;
            await SendJson(res, new GenericResponse { Status = "error", Detail = "Ruta no encontrada" }, AppJsonContext.Default.GenericResponse, 404);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request {path}: {ex}");
            await SendJson(res, new GenericResponse { Status = "error", Detail = ex.Message }, AppJsonContext.Default.GenericResponse, 500);
        }
    }

    private async Task HandleApiRoute(string path, HttpListenerRequest req, HttpListenerResponse res)
    {
        // GET /api/heartbeat
        if (req.HttpMethod == "GET" && path.Equals("/api/heartbeat", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Exchange(ref _lastHeartbeatTicks, DateTime.UtcNow.Ticks);
            await SendJson(res, new GenericResponse { Status = "alive" }, AppJsonContext.Default.GenericResponse);
            return;
        }

        // POST /api/shutdown
        if ((req.HttpMethod == "POST" || req.HttpMethod == "GET") && path.Equals("/api/shutdown", StringComparison.OrdinalIgnoreCase))
        {
            _shutdownSignal.Set();
            await SendJson(res, new GenericResponse { Status = "shutting_down" }, AppJsonContext.Default.GenericResponse);
            return;
        }

        // GET /api/next-number
        if (req.HttpMethod == "GET" && path.Equals("/api/next-number", StringComparison.OrdinalIgnoreCase))
        {
            int next = _db.GetNextRecipeNumber();
            await SendJson(res, new NextNumberResponse
            {
                NextNumber = next,
                NextNumberFormatted = next.ToString("D4")
            }, AppJsonContext.Default.NextNumberResponse);
            return;
        }

        // GET /api/config
        if (req.HttpMethod == "GET" && path.Equals("/api/config", StringComparison.OrdinalIgnoreCase))
        {
            var config = _db.GetDoctorConfig();
            await SendJson(res, config, AppJsonContext.Default.DoctorConfig);
            return;
        }

        // POST /api/config
        if (req.HttpMethod == "POST" && path.Equals("/api/config", StringComparison.OrdinalIgnoreCase))
        {
            string body = await ReadBodyAsString(req);
            var config = JsonSerializer.Deserialize(body, AppJsonContext.Default.DoctorConfig) ?? new DoctorConfig();
            _db.SaveDoctorConfig(config);
            await SendJson(res, new GenericResponse
            {
                Status = "success",
                Message = "Datos del Médico Veterinario guardados correctamente."
            }, AppJsonContext.Default.GenericResponse);
            return;
        }

        // GET /api/recetas
        if (req.HttpMethod == "GET" && path.Equals("/api/recetas", StringComparison.OrdinalIgnoreCase))
        {
            string? query = req.QueryString["q"];
            List<Prescription> list;
            if (!string.IsNullOrWhiteSpace(query))
            {
                list = _db.SearchPrescriptions(query.Trim());
            }
            else
            {
                list = _db.GetRecentPrescriptions(100);
            }
            await SendJson(res, list, AppJsonContext.Default.ListPrescription);
            return;
        }

        // POST /api/recetas
        if (req.HttpMethod == "POST" && path.Equals("/api/recetas", StringComparison.OrdinalIgnoreCase))
        {
            string body = await ReadBodyAsString(req);
            PrescriptionInput? input = null;
            try
            {
                input = JsonSerializer.Deserialize(body, AppJsonContext.Default.PrescriptionInput);
            }
            catch (Exception ex)
            {
                await SendJson(res, new GenericResponse { Status = "error", Detail = $"Formato JSON no válido: {ex.Message}" }, AppJsonContext.Default.GenericResponse, 400);
                return;
            }

            if (input == null)
            {
                await SendJson(res, new GenericResponse { Status = "error", Detail = "Cuerpo de solicitud inválido." }, AppJsonContext.Default.GenericResponse, 400);
                return;
            }

            if (!input.Validate(out string? valError))
            {
                await SendJson(res, new GenericResponse { Status = "error", Detail = valError }, AppJsonContext.Default.GenericResponse, 400);
                return;
            }

            var (createdId, numeroReceta) = _db.SavePrescription(input);
            await SendJson(res, new CreateRecipeResponse
            {
                Id = createdId,
                NumeroReceta = numeroReceta,
                Status = "success",
                Message = $"Receta N° {numeroReceta} emitida exitosamente"
            }, AppJsonContext.Default.CreateRecipeResponse);
            return;
        }

        // Rutas con ID: /api/recetas/{id}
        var matchId = Regex.Match(path, @"^/api/recetas/(\d+)$", RegexOptions.IgnoreCase);
        if (matchId.Success && int.TryParse(matchId.Groups[1].Value, out int id))
        {
            if (req.HttpMethod == "GET")
            {
                var receta = _db.GetPrescriptionById(id);
                if (receta == null)
                {
                    await SendJson(res, new GenericResponse { Status = "error", Detail = "Receta no encontrada" }, AppJsonContext.Default.GenericResponse, 404);
                    return;
                }
                await SendJson(res, receta, AppJsonContext.Default.Prescription);
                return;
            }

            if (req.HttpMethod == "PUT")
            {
                string body = await ReadBodyAsString(req);
                PrescriptionInput? input = null;
                try
                {
                    input = JsonSerializer.Deserialize(body, AppJsonContext.Default.PrescriptionInput);
                }
                catch (Exception ex)
                {
                    await SendJson(res, new GenericResponse { Status = "error", Detail = $"Formato JSON no válido: {ex.Message}" }, AppJsonContext.Default.GenericResponse, 400);
                    return;
                }

                if (input == null)
                {
                    await SendJson(res, new GenericResponse { Status = "error", Detail = "Datos inválidos" }, AppJsonContext.Default.GenericResponse, 400);
                    return;
                }

                if (!input.Validate(out string? valError))
                {
                    await SendJson(res, new GenericResponse { Status = "error", Detail = valError }, AppJsonContext.Default.GenericResponse, 400);
                    return;
                }

                bool updated = _db.UpdatePrescription(id, input);
                if (!updated)
                {
                    await SendJson(res, new GenericResponse { Status = "error", Detail = "Receta no encontrada para actualizar." }, AppJsonContext.Default.GenericResponse, 404);
                    return;
                }

                await SendJson(res, new GenericResponse
                {
                    Status = "success",
                    Message = "Receta actualizada exitosamente."
                }, AppJsonContext.Default.GenericResponse);
                return;
            }

            if (req.HttpMethod == "DELETE")
            {
                bool deleted = _db.DeletePrescription(id);
                await SendJson(res, new GenericResponse
                {
                    Status = "success",
                    Message = deleted ? "Receta eliminada correctamente." : "Registro no encontrado."
                }, AppJsonContext.Default.GenericResponse);
                return;
            }
        }

        // POST /api/recetas/{id}/anular
        var matchAnular = Regex.Match(path, @"^/api/recetas/(\d+)/anular$", RegexOptions.IgnoreCase);
        if (req.HttpMethod == "POST" && matchAnular.Success && int.TryParse(matchAnular.Groups[1].Value, out int anularId))
        {
            string body = await ReadBodyAsString(req);
            var anularInput = JsonSerializer.Deserialize(body, AppJsonContext.Default.AnularInput) ?? new AnularInput();

            bool canceled = _db.CancelPrescription(anularId, anularInput.Motivo);
            if (!canceled)
            {
                await SendJson(res, new GenericResponse { Status = "error", Detail = "No se pudo anular la receta." }, AppJsonContext.Default.GenericResponse, 404);
                return;
            }

            await SendJson(res, new GenericResponse
            {
                Status = "success",
                Message = "Receta anulada exitosamente."
            }, AppJsonContext.Default.GenericResponse);
            return;
        }

        // GET /api/backup-db
        if (req.HttpMethod == "GET" && path.Equals("/api/backup-db", StringComparison.OrdinalIgnoreCase))
        {
            byte[] dbBytes = _db.GetDatabaseBytes();
            string filename = $"recetas_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            res.ContentType = "application/octet-stream";
            res.Headers.Add("Content-Disposition", $"attachment; filename=\"{filename}\"");
            res.ContentLength64 = dbBytes.Length;
            await res.OutputStream.WriteAsync(dbBytes, 0, dbBytes.Length);
            res.Close();
            return;
        }

        // POST /api/restore-db
        if (req.HttpMethod == "POST" && path.Equals("/api/restore-db", StringComparison.OrdinalIgnoreCase))
        {
            byte[] fileBytes = await ExtractFileBytesFromMultipart(req);
            if (fileBytes.Length == 0)
            {
                await SendJson(res, new GenericResponse
                {
                    Status = "error",
                    Detail = "No se recibió ningún archivo para restaurar."
                }, AppJsonContext.Default.GenericResponse, 400);
                return;
            }

            try
            {
                _db.RestoreDatabase(fileBytes);
                await SendJson(res, new GenericResponse
                {
                    Status = "success",
                    Message = "Base de datos restaurada exitosamente."
                }, AppJsonContext.Default.GenericResponse);
            }
            catch (Exception ex)
            {
                await SendJson(res, new GenericResponse
                {
                    Status = "error",
                    Detail = ex.Message
                }, AppJsonContext.Default.GenericResponse, 400);
            }
            return;
        }

        // Ruta de API no reconocida
        await SendJson(res, new GenericResponse { Status = "error", Detail = "Endpoint de API no encontrado." }, AppJsonContext.Default.GenericResponse, 404);
    }

    private async Task ServeIndexHtml(HttpListenerResponse res)
    {
        byte[] htmlBytes;
        string diskPath = Path.Combine(_webRoot, "src", "templates", "index.html");
        if (File.Exists(diskPath))
        {
            htmlBytes = await File.ReadAllBytesAsync(diskPath);
        }
        else
        {
            htmlBytes = GetEmbeddedResourceBytes("index.html");
        }

        res.ContentType = "text/html; charset=utf-8";
        res.ContentLength64 = htmlBytes.Length;
        await res.OutputStream.WriteAsync(htmlBytes, 0, htmlBytes.Length);
        res.Close();
    }

    private async Task ServeStaticFile(string relativePath, HttpListenerResponse res)
    {
        string subPath = relativePath.Substring("/static/".Length).Replace('/', Path.DirectorySeparatorChar);
        string diskPath = Path.Combine(_webRoot, "src", "static", subPath);

        byte[] fileBytes;
        if (File.Exists(diskPath))
        {
            fileBytes = await File.ReadAllBytesAsync(diskPath);
        }
        else
        {
            string fileName = Path.GetFileName(diskPath);
            fileBytes = GetEmbeddedResourceBytes(fileName);
        }

        if (fileBytes.Length == 0)
        {
            res.StatusCode = 404;
            res.Close();
            return;
        }

        string ext = Path.GetExtension(diskPath).ToLowerInvariant();
        res.ContentType = ext switch
        {
            ".css" => "text/css; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".ico" => "image/x-icon",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".json" => "application/json; charset=utf-8",
            _ => "application/octet-stream"
        };

        res.ContentLength64 = fileBytes.Length;
        await res.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
        res.Close();
    }

    private static byte[] GetEmbeddedResourceBytes(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string? resourceName = null;
        foreach (var r in assembly.GetManifestResourceNames())
        {
            if (r.EndsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                resourceName = r;
                break;
            }
        }

        if (resourceName == null) return Array.Empty<byte>();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return Array.Empty<byte>();

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static async Task<string> ReadBodyAsString(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task SendJson<T>(HttpListenerResponse res, T data, JsonTypeInfo<T> jsonTypeInfo, int statusCode = 200)
    {
        try
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, jsonTypeInfo);
            res.StatusCode = statusCode;
            res.ContentType = "application/json; charset=utf-8";
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            res.Close();
        }
        catch (Exception)
        {
            // El cliente desconectó antes de finalizar la escritura
            try { res.Abort(); } catch { }
        }
    }

    private static async Task<byte[]> ExtractFileBytesFromMultipart(HttpListenerRequest req)
    {
        using var ms = new MemoryStream();
        await req.InputStream.CopyToAsync(ms);
        byte[] body = ms.ToArray();

        string? contentType = req.ContentType;
        if (string.IsNullOrEmpty(contentType) ||
            contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
            !contentType.Contains("boundary=", StringComparison.OrdinalIgnoreCase))
        {
            return body;
        }

        string boundary = "--" + contentType.Split("boundary=")[1].Trim().Trim('"');
        byte[] boundaryBytes = Encoding.ASCII.GetBytes(boundary);
        byte[] headerTerminator = Encoding.ASCII.GetBytes("\r\n\r\n");

        int startPos = FindBytes(body, boundaryBytes, 0);
        if (startPos == -1) return body;

        int headersEnd = FindBytes(body, headerTerminator, startPos);
        if (headersEnd == -1) return body;

        int contentStart = headersEnd + 4;
        int nextBoundary = FindBytes(body, boundaryBytes, contentStart);
        if (nextBoundary == -1) nextBoundary = body.Length;

        int contentEnd = nextBoundary;
        if (contentEnd >= 2 && body[contentEnd - 2] == '\r' && body[contentEnd - 1] == '\n')
        {
            contentEnd -= 2;
        }

        int length = Math.Max(0, contentEnd - contentStart);
        byte[] fileBytes = new byte[length];
        Array.Copy(body, contentStart, fileBytes, 0, length);
        return fileBytes;
    }

    private static int FindBytes(byte[] source, byte[] pattern, int startIndex)
    {
        for (int i = startIndex; i <= source.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }
}
