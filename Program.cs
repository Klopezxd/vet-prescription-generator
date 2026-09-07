using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using RecetarioAgrocalidad.Database;
using RecetarioAgrocalidad.Server;

namespace RecetarioAgrocalidad;

public static class Program
{
    private const int Port = 8765;
    private const string TargetUrl = "http://127.0.0.1:8765";

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // 1. Si ya hay una instancia corriendo, abrimos ventana y terminamos
            if (IsPortInUse(Port))
            {
                LaunchBrowserApp(TargetUrl);
                return;
            }

            // 2. Iniciar base de datos SQLite y Servidor HTTP
            var db = new DatabaseManager();
            var server = new HttpServer(db, Port);
            server.Start();

            // 3. Abrir ventana de escritorio nativa
            var browserProcess = LaunchBrowserApp(TargetUrl);

            if (browserProcess != null)
            {
                // Esperar a que el usuario cierre la ventana del programa
                browserProcess.WaitForExit();
            }
            else
            {
                // Si abrió en navegador predeterminado del sistema, esperar señal
                Thread.Sleep(Timeout.Infinite);
            }

            // 4. Detener servidor limpiamente al salir
            server.Stop();
        }
        catch (Exception ex)
        {
            try
            {
                string crashPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RecetarioAgrocalidad",
                    "crash.log"
                );
                Directory.CreateDirectory(Path.GetDirectoryName(crashPath)!);
                File.AppendAllText(crashPath, $"[{DateTime.Now}] Crash: {ex}\n");
            }
            catch { }
        }
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect("127.0.0.1", port, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(150));
            if (!success) return false;
            client.EndConnect(result);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindBrowserExecutable()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        string[] candidates = new[]
        {
            Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(localAppData, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private static Process? LaunchBrowserApp(string url)
    {
        string? browserExe = FindBrowserExecutable();
        if (!string.IsNullOrEmpty(browserExe))
        {
            string profileDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecetarioAgrocalidad",
                "profile"
            );
            Directory.CreateDirectory(profileDir);

            var psi = new ProcessStartInfo
            {
                FileName = browserExe,
                Arguments = $"--app={url} --user-data-dir=\"{profileDir}\" --window-size=1300,850",
                UseShellExecute = false
            };

            return Process.Start(psi);
        }

        // Fallback al navegador predeterminado
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
        return null;
    }
}
