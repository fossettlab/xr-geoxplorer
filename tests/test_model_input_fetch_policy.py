"""HTTPS, redirect and companion-origin policy tests. Not importer cancellation."""

from __future__ import annotations

import http.client
import importlib.util
import ssl
import sys
import unittest
from pathlib import Path
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[1]


def load(name: str, relative: str):
    path = ROOT / relative
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


HTTP = load("model_input_http_fixture", "scripts/model_input_http_fixture.py")
POLICY = load("model_input_fetch_policy", "scripts/model_input_fetch_policy.py")
PAYLOAD = b"authored transport fixture bytes"


def requester(cert: Path | None):
    context = None
    if cert is not None:
        context = ssl.create_default_context()
        context.load_verify_locations(cert)

    def request(url: str, headers: dict[str, str]):
        parts = urlsplit(url)
        if parts.scheme == "https":
            connection = http.client.HTTPSConnection(
                parts.hostname, parts.port, context=context, timeout=2
            )
        else:
            connection = http.client.HTTPConnection(
                parts.hostname, parts.port, timeout=2
            )
        try:
            connection.request("GET", parts.path or "/", headers=headers)
            response = connection.getresponse()
            body = response.read()
            return response.status, dict(response.getheaders()), body
        finally:
            connection.close()

    return request


class FetchPolicyTests(unittest.TestCase):
    def test_invalid_budgets_refuse_before_request(self) -> None:
        for value in [float("nan"), float("inf"), 1.5, True, -1]:
            with self.subTest(value=value):

                def no_request(*args):
                    self.fail("Invalid budget reached transport")

                with self.assertRaises(POLICY.PolicyError) as caught:
                    POLICY.fetch_public_model(
                        "https://example.invalid/model.glb",
                        POLICY.PublicModelPolicy(max_bytes=value),
                        request=no_request,
                    )
                self.assertEqual(caught.exception.code, "invalid_budget")

    def test_invalid_redirect_limits_refuse_before_request(self) -> None:
        for value in [float("nan"), float("inf"), 1.5, True, -1]:
            with self.subTest(value=value):

                def no_request(*args):
                    self.fail("Invalid limit reached transport")

                with self.assertRaises(POLICY.PolicyError) as caught:
                    POLICY.fetch_public_model(
                        "https://example.invalid/model.glb",
                        POLICY.PublicModelPolicy(max_bytes=1024, max_redirects=value),
                        request=no_request,
                    )
                self.assertEqual(caught.exception.code, "invalid_redirect_limit")

    def test_http_root_is_rejected_when_https_is_required(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=1024)
        with self.assertRaises(POLICY.PolicyError) as caught:
            policy.check_root("http://127.0.0.1/triangle.glb")
        self.assertEqual(caught.exception.code, "https_required")
        self.assertEqual(caught.exception.redacted, "http://127.0.0.1/triangle.glb")

    def test_file_urls_are_rejected(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=1024)
        with self.assertRaises(POLICY.PolicyError) as caught:
            policy.check_root("file:///tmp/triangle.glb")
        self.assertEqual(caught.exception.code, "local_file_forbidden")

    def test_credentials_are_stripped_from_logged_urls(self) -> None:
        redacted = POLICY.redact_url(
            "https://token:secret@example.test/model.glb?sig=abc"
        )
        self.assertEqual(redacted, "https://example.test/model.glb")
        self.assertNotIn("token", redacted)
        self.assertNotIn("secret", redacted)
        self.assertNotIn("sig=", redacted)

    def test_ipv6_redaction_preserves_a_valid_bracketed_host(self) -> None:
        redacted = POLICY.redact_url("https://[::1]:8443/model.glb?sig=abc")
        self.assertEqual(redacted, "https://[::1]:8443/model.glb")

    def test_malformed_url_is_a_redacted_policy_error(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=1024)
        with self.assertRaises(POLICY.PolicyError) as caught:
            policy.check_root("https://token@example.test:secret/model.glb")
        self.assertEqual(caught.exception.code, "invalid_url")
        self.assertEqual(caught.exception.redacted, "<invalid-url>")
        self.assertNotIn("token", str(caught.exception))
        self.assertNotIn("secret", str(caught.exception))

    def test_unconfigured_budget_is_not_unlimited(self) -> None:
        policy = POLICY.PublicModelPolicy()
        with self.assertRaises(POLICY.PolicyError) as caught:
            policy.require_budget()
        self.assertEqual(caught.exception.code, "budget_unconfigured")

    def test_unconfigured_budget_fails_before_requesting(self) -> None:
        requested = False

        def request(_url: str, _headers: dict[str, str]):
            nonlocal requested
            requested = True
            return 200, {}, PAYLOAD

        with self.assertRaises(POLICY.PolicyError) as caught:
            POLICY.fetch_public_model(
                "https://models.example/triangle.glb",
                POLICY.PublicModelPolicy(),
                request=request,
            )
        self.assertEqual(caught.exception.code, "budget_unconfigured")
        self.assertFalse(requested)

    def test_https_complete_response_and_same_origin_redirect(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD))
        routes = {
            "/triangle.glb": HTTP.Route(body=PAYLOAD, content_type="model/gltf-binary"),
            "/redirect": HTTP.Route(status=302, location="/triangle.glb"),
        }
        try:
            context = HTTP.serve_routes(routes, tls=True)
            url, cert = self.enterContext(context)
        except PermissionError as exc:
            self.skipTest(f"Loopback listener prohibited: {exc}")
        result = POLICY.fetch_public_model(
            url + "/redirect", policy, request=requester(cert)
        )
        self.assertEqual(result.body, PAYLOAD)
        self.assertEqual(result.redirects, 1)
        self.assertEqual(result.received, len(PAYLOAD))
        self.assertFalse(result.authorization_forwarded)

    def test_https_to_http_redirect_is_rejected(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD))
        try:
            https_ctx = HTTP.serve_routes(
                {"/downgrade": HTTP.Route(status=302, location="http://127.0.0.1/no")},
                tls=True,
            )
            url, cert = self.enterContext(https_ctx)
        except PermissionError as exc:
            self.skipTest(f"Loopback listener prohibited: {exc}")
        with self.assertRaises(POLICY.PolicyError) as caught:
            POLICY.fetch_public_model(
                url + "/downgrade", policy, request=requester(cert)
            )
        self.assertEqual(caught.exception.code, "https_required")

    def test_authorization_is_not_forwarded_across_origins(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD))
        seen: list[str | None] = []

        try:
            target_ctx = HTTP.serve_routes(
                {"/triangle.glb": HTTP.Route(body=PAYLOAD)},
                tls=True,
            )
            target, target_cert = self.enterContext(target_ctx)
            source_ctx = HTTP.serve_routes(
                {"/leave": HTTP.Route(status=302, location=target + "/triangle.glb")},
                tls=True,
            )
            source, source_cert = self.enterContext(source_ctx)
        except PermissionError as exc:
            self.skipTest(f"Loopback listener prohibited: {exc}")

        def request(url: str, headers: dict[str, str]):
            seen.append(headers.get("Authorization"))
            cert = (
                source_cert
                if urlsplit(url).port == urlsplit(source).port
                else target_cert
            )
            return requester(cert)(url, headers)

        result = POLICY.fetch_public_model(
            source + "/leave",
            policy,
            request=request,
            authorization="Bearer test-token",
        )
        self.assertEqual(result.body, PAYLOAD)
        self.assertEqual(seen, ["Bearer test-token", None])
        self.assertFalse(result.authorization_forwarded)
        self.assertNotIn("test-token", result.url)

    def test_authorization_stays_stripped_after_a_cross_origin_redirect(self) -> None:
        source = "https://models.example/start.glb"
        other = "https://cdn.example/bounce"
        returned = "https://models.example/final.glb"
        seen: list[tuple[str, str | None]] = []

        def request(url: str, headers: dict[str, str]):
            seen.append((url, headers.get("Authorization")))
            if url == source:
                return 302, {"Location": other, "Content-Length": "0"}, b""
            if url == other:
                return 302, {"Location": returned, "Content-Length": "0"}, b""
            return 200, {"Content-Length": str(len(PAYLOAD))}, PAYLOAD

        result = POLICY.fetch_public_model(
            source,
            POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD)),
            request=request,
            authorization="Bearer test-token",
        )
        self.assertEqual(
            seen,
            [
                (source, "Bearer test-token"),
                (other, None),
                (returned, None),
            ],
        )
        self.assertFalse(result.authorization_forwarded)

    def test_first_route_rejects_companion_and_cross_origin_mode_is_explicit(
        self,
    ) -> None:
        root = "https://models.example/scene.glb"
        companion = "https://cdn.example/pixel.png"
        first = POLICY.PublicModelPolicy(max_bytes=1024)
        with self.assertRaises(POLICY.PolicyError) as caught:
            first.check_companion(root, companion)
        self.assertEqual(caught.exception.code, "external_reference_forbidden")

        later = POLICY.PublicModelPolicy(max_bytes=1024, allow_companions=True)
        with self.assertRaises(POLICY.PolicyError) as caught:
            later.check_companion(root, companion)
        self.assertEqual(caught.exception.code, "companion_origin_mismatch")

        same = POLICY.PublicModelPolicy(max_bytes=1024, allow_companions=True)
        self.assertEqual(
            same.check_companion(root, "https://models.example/pixel.png"),
            "https://models.example/pixel.png",
        )
        self.assertEqual(
            same.check_companion(root, "https://models.example:443/pixel.png"),
            "https://models.example:443/pixel.png",
        )

    def test_actual_received_bytes_are_counted_without_content_length(self) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD))
        try:
            context = HTTP.serve_routes(
                {
                    "/no-length": HTTP.Route(
                        body=PAYLOAD, omit_length=True, content_type="model/gltf-binary"
                    )
                },
                tls=True,
            )
            url, cert = self.enterContext(context)
        except PermissionError as exc:
            self.skipTest(f"Loopback listener prohibited: {exc}")
        result = POLICY.fetch_public_model(
            url + "/no-length", policy, request=requester(cert)
        )
        self.assertIsNone(result.declared)
        self.assertEqual(result.received, len(PAYLOAD))
        self.assertEqual(result.body, PAYLOAD)

    def test_declared_oversize_is_rejected_before_treating_the_body_as_accepted(
        self,
    ) -> None:
        policy = POLICY.PublicModelPolicy(max_bytes=4)
        with self.assertRaises(POLICY.PolicyError) as caught:
            policy.count_received(2, 50)
        self.assertEqual(caught.exception.code, "declared_budget_exceeded")

    def test_redirect_response_bytes_count_toward_the_operation_budget(self) -> None:
        start = "https://models.example/start.glb"
        final = "https://models.example/final.glb"

        def request(url: str, _headers: dict[str, str]):
            if url == start:
                return 302, {"Location": final}, b"redirect-body"
            return 200, {}, PAYLOAD

        policy = POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD))
        with self.assertRaises(POLICY.PolicyError) as caught:
            POLICY.fetch_public_model(start, policy, request=request)
        self.assertEqual(caught.exception.code, "budget_exceeded")

    def test_invalid_content_length_is_a_policy_error(self) -> None:
        def request(_url: str, _headers: dict[str, str]):
            return 200, {"Content-Length": "unknown"}, PAYLOAD

        with self.assertRaises(POLICY.PolicyError) as caught:
            POLICY.fetch_public_model(
                "https://models.example/triangle.glb",
                POLICY.PublicModelPolicy(max_bytes=len(PAYLOAD)),
                request=request,
            )
        self.assertEqual(caught.exception.code, "invalid_content_length")


if __name__ == "__main__":
    unittest.main()
