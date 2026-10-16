"""Check actual loopback responses, separately from future importer tests."""

import http.client
import importlib.util
import sys
import unittest
from pathlib import Path
from urllib.parse import urlsplit

SCRIPT = Path(__file__).resolve().parents[1] / "scripts/model_input_http_fixture.py"
SPEC = importlib.util.spec_from_file_location("model_input_http_fixture", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)
# Authored transport bytes; these tests inspect HTTP behavior, not GLB validity.
PAYLOAD = b"authored transport fixture bytes"
TEST_SOCKET_TIMEOUT_SECONDS = 1


class HttpFixtureTests(unittest.TestCase):
    def setUp(self) -> None:
        try:
            self.url = self.enterContext(MODULE.serve_fixture(PAYLOAD))
        except PermissionError as exc:
            self.skipTest(f"Loopback listener prohibited: {exc}")
        self.connection = http.client.HTTPConnection(
            "127.0.0.1", urlsplit(self.url).port, timeout=TEST_SOCKET_TIMEOUT_SECONDS
        )
        self.addCleanup(self.connection.close)

    def test_complete_response(self) -> None:
        self.connection.request("GET", "/triangle.glb")
        response = self.connection.getresponse()
        self.assertEqual(response.status, 200)
        self.assertEqual(int(response.getheader("Content-Length")), len(PAYLOAD))
        self.assertEqual(response.read(), PAYLOAD)

    def test_missing_length_is_close_delimited(self) -> None:
        self.connection.request("GET", "/no-length")
        response = self.connection.getresponse()
        self.assertIsNone(response.getheader("Content-Length"))
        self.assertEqual(response.read(), PAYLOAD)

    def test_redirect_targets_and_loop(self) -> None:
        for route, target in [
            ("/redirect", "/triangle.glb"),
            ("/redirect-loop", "/redirect-loop"),
        ]:
            self.connection.request("GET", route)
            response = self.connection.getresponse()
            self.assertEqual(response.status, 302)
            self.assertEqual(response.getheader("Location"), target)
            self.assertEqual(response.read(), b"")

    def test_truncated_body_is_observable(self) -> None:
        self.connection.request("GET", "/truncated")
        response = self.connection.getresponse()
        with self.assertRaises(http.client.IncompleteRead) as caught:
            response.read()
        self.assertEqual(caught.exception.partial, PAYLOAD[: len(PAYLOAD) // 2])

    def test_stall_times_out_for_the_client(self) -> None:
        self.connection.request("GET", "/stall")
        response = self.connection.getresponse()
        with self.assertRaises(TimeoutError):
            response.read()

    def test_unlisted_paths_are_not_served(self) -> None:
        self.connection.request("GET", "/../manifest.json")
        response = self.connection.getresponse()
        self.assertEqual(response.status, 404)
        response.read()


if __name__ == "__main__":
    unittest.main()
