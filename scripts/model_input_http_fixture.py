"""Loopback-only HTTP/HTTPS fault fixtures; never used as the production URL policy.

Serves only supplied fixture bytes, without directory access or logging request
URLs. Plain HTTP remains available as a transport fault. HTTPS uses a temporary
localhost certificate for policy tests. Neither mode is headset connectivity,
importer cancellation, or permission to relax the public HTTPS requirement.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import ssl
import subprocess
import tempfile
from collections.abc import Iterator
from contextlib import contextmanager
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import Event, Thread
from urllib.parse import urlsplit


def read_triangle(directory: Path) -> bytes:
    """Load only a generated triangle whose bytes match its fixture manifest."""
    asset = directory / "triangle.glb"
    if asset.is_symlink():
        raise ValueError("The fixture asset must not be a symlink")
    manifest = json.loads((directory / "manifest.json").read_text())
    entry = next(item for item in manifest["files"] if item["path"] == "triangle.glb")
    data = asset.read_bytes()
    if (
        len(data) != entry["bytes"]
        or hashlib.sha256(data).hexdigest() != entry["sha256"]
    ):
        raise ValueError("Triangle differs from its generated manifest")
    return data


class Route:
    """One exact path. Unknown paths stay 404 and never list the filesystem."""

    def __init__(
        self,
        body: bytes = b"",
        status: int = 200,
        content_type: str = "application/octet-stream",
        location: str | None = None,
        omit_length: bool = False,
        truncate: bool = False,
        stall: bool = False,
    ) -> None:
        self.body = body
        self.status = status
        self.content_type = content_type
        self.location = location
        self.omit_length = omit_length
        self.truncate = truncate
        self.stall = stall


def write_localhost_cert(directory: Path) -> tuple[Path, Path]:
    """Create a one-day 127.0.0.1 certificate for loopback TLS tests."""
    cert = directory / "cert.pem"
    key = directory / "key.pem"
    subprocess.run(
        [
            "openssl",
            "req",
            "-x509",
            "-newkey",
            "rsa:2048",
            "-keyout",
            str(key),
            "-out",
            str(cert),
            "-days",
            "1",
            "-nodes",
            "-subj",
            "/CN=127.0.0.1",
            "-addext",
            "subjectAltName=IP:127.0.0.1",
        ],
        check=True,
        capture_output=True,
    )
    return cert, key


@contextmanager
def serve_routes(
    routes: dict[str, Route],
    *,
    tls: bool = False,
) -> Iterator[tuple[str, Path | None]]:
    """Serve named routes on an OS-assigned loopback port."""
    stopped = Event()

    class Handler(BaseHTTPRequestHandler):
        protocol_version = "HTTP/1.0"

        def log_message(self, format: str, *args: object) -> None:
            pass  # Do not record request paths or query strings.

        def do_GET(self) -> None:
            route = urlsplit(self.path).path
            spec = routes.get(route)
            if spec is None:
                self.send_error(404, "Unknown fixture")
                return
            self.send_response(spec.status)
            if spec.location is not None:
                self.send_header("Location", spec.location)
            if spec.status in {301, 302, 303, 307, 308}:
                self.send_header("Content-Length", "0")
                self.end_headers()
                return
            self.send_header("Content-Type", spec.content_type)
            # Truncation declares the full length, then writes half, so clients
            # observe IncompleteRead the same way as the original HTTP fixture.
            if not spec.omit_length:
                self.send_header("Content-Length", str(len(spec.body)))
            self.end_headers()
            self.close_connection = True
            if spec.stall:
                stopped.wait()
                return
            payload = spec.body[: len(spec.body) // 2] if spec.truncate else spec.body
            try:
                self.wfile.write(payload)
            except (BrokenPipeError, ConnectionResetError):
                pass  # Client cancellation is a deliberate fixture use case.

    server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
    cert_home = None
    cert_path = None
    if tls:
        cert_home = tempfile.TemporaryDirectory()
        cert_path, key_path = write_localhost_cert(Path(cert_home.name))
        context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
        context.load_cert_chain(cert_path, key_path)
        server.socket = context.wrap_socket(server.socket, server_side=True)
    worker = Thread(target=server.serve_forever, daemon=True)
    worker.start()
    scheme = "https" if tls else "http"
    try:
        yield f"{scheme}://127.0.0.1:{server.server_port}", cert_path
    finally:
        stopped.set()
        server.shutdown()
        server.server_close()
        worker.join()
        if cert_home is not None:
            cert_home.cleanup()


@contextmanager
def serve_fixture(data: bytes) -> Iterator[str]:
    """Serve the original triangle transport variants until the context exits."""
    routes = {
        "/triangle.glb": Route(body=data, content_type="model/gltf-binary"),
        "/no-length": Route(
            body=data, content_type="model/gltf-binary", omit_length=True
        ),
        "/truncated": Route(body=data, content_type="model/gltf-binary", truncate=True),
        "/stall": Route(body=data, content_type="model/gltf-binary", stall=True),
        "/redirect": Route(status=302, location="/triangle.glb"),
        "/redirect-loop": Route(status=302, location="/redirect-loop"),
    }
    with serve_routes(routes) as (url, _cert):
        yield url


def main() -> None:
    """Run the loopback fixture server until interrupted."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--directory",
        type=Path,
        default=Path(__file__).resolve().parents[1]
        / "tests/fixtures/model-inputs/generated",
    )
    args = parser.parse_args()
    with serve_fixture(read_triangle(args.directory.resolve())) as url:
        print(f"Fixture server: {url}", flush=True)
        print(
            "Routes: /triangle.glb /redirect /redirect-loop /no-length /truncated /stall",
            flush=True,
        )
        try:
            Event().wait()
        except KeyboardInterrupt:
            pass


if __name__ == "__main__":
    main()
