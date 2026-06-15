#!/usr/bin/env bash
# Sends RakNet UnconnectedPing and prints MOTD if the server responds.
set -euo pipefail

HOST="${1:-127.0.0.1}"
PORT="${2:-19132}"

export ORION_PING_HOST="$HOST"
export ORION_PING_PORT="$PORT"

python3 <<'PY'
import os
import socket
import struct
import sys
import time

MAGIC = bytes([
    0x00, 0xFF, 0xFF, 0x00, 0xFE, 0xFE, 0xFE, 0xFE,
    0xFD, 0xFD, 0xFD, 0xFD, 0x12, 0x34, 0x56, 0x78,
])

host = os.environ["ORION_PING_HOST"]
port = int(os.environ["ORION_PING_PORT"])

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.settimeout(3.0)

ts = int(time.time() * 1000) & 0xFFFFFFFFFFFFFFFF
ping = bytearray([0x01])
ping += struct.pack(">q", ts)
ping += MAGIC
ping += struct.pack(">q", 0)

try:
    sock.sendto(ping, (host, port))
    data, _ = sock.recvfrom(2048)
except socket.timeout:
    print(f"FAIL: sem resposta UnconnectedPong de {host}:{port}")
    print("  - Servidor rodando? (dotnet run --project src/Server)")
    print("  - Porta UDP 19132 livre? (ss -ulnp | grep 19132)")
    sys.exit(1)
except OSError as ex:
    print(f"FAIL: erro de socket para {host}:{port}: {ex}")
    sys.exit(1)

if len(data) < 35 or data[0] != 0x1C:
    print(f"FAIL: pacote inesperado (id=0x{data[0]:02X} len={len(data)})")
    sys.exit(1)

motd_len = struct.unpack(">H", data[33:35])[0]
if len(data) < 35 + motd_len:
    print("FAIL: MOTD truncado na resposta")
    sys.exit(1)

motd = data[35 : 35 + motd_len].decode("utf-8", errors="replace")
parts = motd.split(";")

print(f"OK: MOTD de {host}:{port}")
print(f"  {motd}")

exit_code = 0
if len(parts) >= 3:
    print(f"  protocolo={parts[2]} versão={parts[3] if len(parts) > 3 else '?'}")
if "975" not in motd:
    print("WARN: protocolo 975 não encontrado no MOTD (cliente 1.26.20 pode recusar)")
    exit_code = 2

sys.exit(exit_code)
PY
