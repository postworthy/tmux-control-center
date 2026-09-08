#!/usr/bin/env python3
"""Exercise a self-contained publish against a disposable tmux server on loopback.

Usage: python3 tests/native-server-smoke.py /absolute/publish /absolute/tmux
Uses no live sessions, persisted credentials, external network or Python packages.
"""
import base64
import http.client
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import sys
import tempfile
import time

publish = Path(sys.argv[1]).resolve()
tmux = str(Path(sys.argv[2]).resolve())
state = Path(tempfile.mkdtemp(prefix="tmuxctl-native-smoke-"))
socket_name = "tmuxctl-smoke-" + secrets.token_hex(8)
server = None
ws = None
passed = False


def tmux_run(*args):
    return subprocess.run([tmux, "-f", "/dev/null", "-L", socket_name, *args],
                          check=True, capture_output=True, text=True, timeout=10).stdout.strip()


def request(path, method="GET", body=None, cookie="", forwarded=True):
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=5)
    headers = {"Host": "localhost", "Origin": "https://localhost", "Cookie": cookie}
    if forwarded:
        headers["X-Forwarded-Proto"] = "https"
    if body is not None:
        headers["Content-Type"] = "application/json"
        body = json.dumps(body)
    connection.request(method, path, body, headers)
    response = connection.getresponse()
    result = response.status, response.getheaders(), response.read()
    connection.close()
    return result


try:
    tmux_run("new-session", "-d", "-s", "smoke", "/bin/sh")
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 0))
        port = reservation.getsockname()[1]
    config = {
        "Urls": f"http://127.0.0.1:{port}", "AllowedHosts": "localhost",
        "Tmux": {"ExecutablePath": tmux, "SocketName": socket_name},
        "Authentication": {"Mode": "ApiKey", "ApiKey": secrets.token_hex(32)},
        "Security": {"AllowedOrigins": ["https://localhost"], "ExternalHttpsTermination": True},
        "ForwardedHeaders": {"Enabled": True, "KnownProxies": ["127.0.0.1"]},
        "Audit": {"Destination": str(state / "audit" / "audit.jsonl")},
        "DataProtection": {"KeysDirectory": str(state / "keys")},
        "WorkspaceRecovery": {"Enabled": False}
    }
    config_path = state / "appsettings.json"
    config_path.write_text(json.dumps(config))
    config_path.chmod(0o600)
    env = {k: v for k, v in os.environ.items() if not (
        k.startswith(("ASPNETCORE_", "DOTNET_")) or "__" in k)}
    env["ASPNETCORE_ENVIRONMENT"] = "Production"
    with (state / "server.log").open("wb") as log:
        server = subprocess.Popen([str(publish / "TmuxMobile.Server"), "--contentRoot", str(state),
                                   "--webroot", str(publish / "wwwroot")], cwd=state,
                                  env=env, stdout=log, stderr=subprocess.STDOUT)
    for _ in range(60):
        if server.poll() is not None:
            raise RuntimeError(f"Published server exited; private logs: {state}")
        try:
            if request("/health/live")[0] == 200:
                break
        except OSError:
            pass
        time.sleep(0.25)
    else:
        raise RuntimeError("Published server did not become live")
    assert request("/health/ready")[0] == 200
    assert request("/api/sessions", forwarded=False)[0] == 426
    assert request("/api/sessions")[0] == 401
    status, headers, _ = request("/api/auth/login", "POST", {"apiKey": config["Authentication"]["ApiKey"]})
    assert status == 204
    cookie = "; ".join(value.split(";", 1)[0] for key, value in headers if key.lower() == "set-cookie")
    assert cookie
    status, _, body = request("/api/sessions", cookie=cookie)
    assert status == 200
    sessions = json.loads(body)
    session_id = next(session["id"] for session in sessions if session["name"] == "smoke")
    assert request("/desktop/", cookie=cookie)[0] == 200
    ws = socket.create_connection(("127.0.0.1", port), timeout=10)
    key = base64.b64encode(os.urandom(16)).decode()
    handshake = (f"GET /ws/terminal/{session_id} HTTP/1.1\r\nHost: localhost\r\n"
                 f"Upgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: {key}\r\n"
                 f"Sec-WebSocket-Version: 13\r\nOrigin: https://localhost\r\n"
                 f"X-Forwarded-Proto: https\r\nCookie: {cookie}\r\n\r\n")
    ws.sendall(handshake.encode())
    response = b""
    while b"\r\n\r\n" not in response:
        response += ws.recv(8192)
        assert len(response) < 65536
    assert response.startswith(b"HTTP/1.1 101 ")
    payload = json.dumps({"type": "resize", "cols": 119, "rows": 45}).encode()
    mask = os.urandom(4)
    ws.sendall(bytes([0x81, 0x80 | len(payload)]) + mask +
               bytes(value ^ mask[i % 4] for i, value in enumerate(payload)))
    for _ in range(100):
        if tmux_run("list-clients", "-F", "#{client_width} #{client_height}") == "119 45":
            break
        time.sleep(0.05)
    else:
        raise RuntimeError("Published native PTY did not resize its real tmux client")
    ws.close()
    ws = None
    server.terminate()
    server.wait(timeout=20)
    assert tmux_run("has-session", "-t", "smoke") == ""
    assert tmux_run("list-clients", "-F", "#{client_pid}") == ""
    assert (state / "audit" / "audit.jsonl").stat().st_mode & 0o777 == 0o600
    assert (state / "keys").stat().st_mode & 0o777 == 0o700
    passed = True
    print("Published server: auth, HTTP denial, assets, real PTY resize, private storage and session-preserving shutdown passed.")
finally:
    if ws:
        ws.close()
    if server and server.poll() is None:
        server.terminate()
        try:
            server.wait(timeout=20)
        except subprocess.TimeoutExpired:
            server.kill()
            server.wait(timeout=5)
    subprocess.run([tmux, "-L", socket_name, "kill-server"], capture_output=True, timeout=10)
    if passed:
        shutil.rmtree(state)
