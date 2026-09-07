"""
Lanzador de escritorio nativo para el Generador de Recetas Veterinarias (Agrocalidad Ecuador).
Inicia el servidor FastAPI en segundo plano y abre la interfaz en modo aplicación nativa de Windows.
"""

import asyncio
import os
import socket
import subprocess
import sys
import threading
import time
import urllib.request
import webbrowser
from pathlib import Path

# En modo --noconsole de PyInstaller en Windows, stdout y stderr son None.
# Redirigir para evitar que formateadores de logging lancen AttributeError.
if sys.stdout is None:
    sys.stdout = open(os.devnull, "w", encoding="utf-8")
if sys.stderr is None:
    sys.stderr = open(os.devnull, "w", encoding="utf-8")


import uvicorn

from src.api import app

PORT = 8765
HOST = "127.0.0.1"


def is_port_in_use(port: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        return s.connect_ex((HOST, port)) == 0


def find_browser_executable() -> str | None:
    candidates = [
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\Edge\Application\msedge.exe"),
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
        os.path.expandvars(r"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"),
    ]
    for path in candidates:
        if Path(path).is_file():
            return path
    return None


def wait_for_server(url: str, timeout: float = 5.0) -> bool:
    start_time = time.time()
    while time.time() - start_time < timeout:
        try:
            with urllib.request.urlopen(url, timeout=1) as response:
                if response.status == 200:
                    return True
        except Exception:
            time.sleep(0.1)
    return False


def main() -> None:
    target_url = f"http://{HOST}:{PORT}"

    # Si ya hay una instancia corriendo, simplemente abrimos la ventana y salimos
    if is_port_in_use(PORT):
        browser_exe = find_browser_executable()
        if browser_exe:
            profile_dir = Path(os.path.expandvars(r"%LOCALAPPDATA%\RecetarioAgrocalidad\profile"))
            profile_dir.mkdir(parents=True, exist_ok=True)
            subprocess.Popen([
                browser_exe,
                f"--app={target_url}",
                f"--user-data-dir={profile_dir}",
                "--window-size=1300,850",
            ])
        else:
            webbrowser.open(target_url)
        return

    # Iniciar servidor Uvicorn en un hilo en segundo plano
    config = uvicorn.Config(
        app=app,
        host=HOST,
        port=PORT,
        log_level="warning",
        access_log=False,
        log_config=None,
    )
    server = uvicorn.Server(config)

    def run_server() -> None:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        loop.run_until_complete(server.serve())

    server_thread = threading.Thread(target=run_server, daemon=True)
    server_thread.start()

    # Esperar a que el servidor responda
    wait_for_server(f"{target_url}/api/config")


    browser_exe = find_browser_executable()
    if browser_exe:
        profile_dir = Path(os.path.expandvars(r"%LOCALAPPDATA%\RecetarioAgrocalidad\profile"))
        profile_dir.mkdir(parents=True, exist_ok=True)
        cmd = [
            browser_exe,
            f"--app={target_url}",
            f"--user-data-dir={profile_dir}",
            "--window-size=1300,850",
        ]
        browser_proc = subprocess.Popen(cmd)
        # Esperar a que el usuario cierre la ventana de la app
        browser_proc.wait()
        server.should_exit = True
    else:
        webbrowser.open(target_url)
        try:
            while server_thread.is_alive():
                time.sleep(1)
        except KeyboardInterrupt:
            server.should_exit = True


if __name__ == "__main__":
    try:
        main()
    except Exception:
        import traceback
        crash_log = Path(os.path.expandvars(r"%LOCALAPPDATA%\RecetarioAgrocalidad\crash.log"))
        crash_log.parent.mkdir(parents=True, exist_ok=True)
        with open(crash_log, "a", encoding="utf-8") as f:
            f.write(f"--- Crash {time.ctime()} ---\n")
            f.write(traceback.format_exc() + "\n")

