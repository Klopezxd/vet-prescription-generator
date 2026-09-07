using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            // 3. Abrir ventana de escritorio en modo aplicación
            LaunchBrowserApp(TargetUrl);

            // 4. Mantener la aplicación viva mientras la ventana esté abierta
            server.WaitForShutdown();

            // 5. Detener servidor limpiamente al salir
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
                File.AppendAllText(crashPath, $"[{DateTime.Now}] {ex}\n\n");
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

    private static void LaunchBrowserApp(string url)
    {
        string? browserExe = FindBrowserExecutable();
        if (!string.IsNullOrEmpty(browserExe))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{browserExe}\" --app={url} --start-maximized",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                return;
            }
            catch { }
        }

        // Fallback al navegador predeterminado
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
