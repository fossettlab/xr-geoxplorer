"""Testable public-model fetch rules. This is not Unity runtime enforcement.

The first public entry is HTTPS GLB without companion fetches. Redirects are
validated before they are followed. HTTPS is preserved. Authorization is never
forwarded across origins. An unconfigured byte budget is a missing prerequisite,
not an unlimited download. Error text uses redacted URLs only.
"""

from __future__ import annotations

from dataclasses import dataclass
from urllib.parse import urljoin, urlsplit, urlunsplit


class PolicyError(Exception):
    """A named policy refusal. `redacted` is safe to log."""

    def __init__(self, code: str, detail: str, url: str | None = None) -> None:
        self.code = code
        self.detail = detail
        self.redacted = redact_url(url) if url else None
        super().__init__(f"{code}: {detail}")


def redact_url(url: str) -> str:
    """Drop userinfo, query and fragment so credentials cannot leak into logs."""
    try:
        parts = urlsplit(url)
        port = parts.port
    except ValueError:
        return "<invalid-url>"
    hostname = parts.hostname or ""
    host = f"[{hostname}]" if ":" in hostname else hostname
    if port:
        host = f"{host}:{port}"
    return urlunsplit((parts.scheme, host, parts.path, "", ""))


def origin_of(url: str) -> tuple[str, str, int | None]:
    """Return scheme, hostname and port for origin comparison."""
    parts = urlsplit(url)
    scheme = parts.scheme.lower()
    port = parts.port
    if port is None:
        port = {"http": 80, "https": 443}.get(scheme)
    return (scheme, (parts.hostname or "").lower(), port)


@dataclass(frozen=True)
class PublicModelPolicy:
    """Bounded first-route rules. Companion fetching is a later explicit mode."""

    require_https: bool = True
    max_redirects: int = 5
    max_bytes: int | None = None
    allow_companions: bool = False
    same_origin_companions_only: bool = True

    def check_url(self, url: str, *, role: str) -> None:
        try:
            parts = urlsplit(url)
            hostname = parts.hostname
            parts.port
        except ValueError:
            raise PolicyError("invalid_url", f"{role} URL is malformed", url) from None
        scheme = parts.scheme.lower()
        if scheme == "file":
            raise PolicyError(
                "local_file_forbidden",
                f"{role} file URLs are a separate platform-granted path",
                url,
            )
        if scheme in {"http", "https"} and not hostname:
            raise PolicyError("invalid_url", f"{role} is missing a host", url)
        if self.require_https and scheme != "https":
            raise PolicyError("https_required", f"{role} must use HTTPS", url)
        if scheme not in {"https", "http"}:
            raise PolicyError(
                "unsupported_scheme", f"{role} scheme is not allowed", url
            )

    def check_root(self, url: str) -> None:
        self.check_url(url, role="root")

    def resolve_redirect(self, current: str, location: str | None) -> str:
        if not location:
            raise PolicyError("invalid_redirect", "Redirect omitted Location", current)
        target = urljoin(current, location)
        self.check_url(target, role="redirect")
        return target

    def check_companion(self, root: str, companion: str) -> str:
        target = urljoin(root, companion)
        if not self.allow_companions:
            raise PolicyError(
                "external_reference_forbidden",
                "The first public route rejects companion resource fetches",
                target,
            )
        self.check_url(target, role="companion")
        if self.same_origin_companions_only and origin_of(root) != origin_of(target):
            raise PolicyError(
                "companion_origin_mismatch",
                "Companion origin differs from the root model",
                target,
            )
        return target

    def require_budget(self) -> int:
        if self.max_bytes is None:
            raise PolicyError(
                "budget_unconfigured",
                "An unconfigured byte budget is not an unlimited download",
            )
        if type(self.max_bytes) is not int or self.max_bytes < 0:
            raise PolicyError(
                "invalid_budget", "Byte budget must be a non-negative integer"
            )
        return self.max_bytes

    def count_received(
        self,
        received: int,
        declared: int | None,
        *,
        already_received: int = 0,
    ) -> int:
        """Count response bytes against the aggregate operation budget."""
        budget = self.require_budget()
        total_received = already_received + received
        if total_received > budget:
            raise PolicyError(
                "budget_exceeded",
                f"Received {total_received} bytes exceeds the configured budget",
            )
        if declared is not None and already_received + declared > budget:
            raise PolicyError(
                "declared_budget_exceeded",
                "Declared response length exceeds the remaining configured budget",
            )
        return total_received


@dataclass(frozen=True)
class FetchResult:
    url: str
    status: int
    body: bytes
    redirects: int
    received: int
    declared: int | None
    authorization_forwarded: bool


def fetch_public_model(
    url: str,
    policy: PublicModelPolicy,
    *,
    request,
    authorization: str | None = None,
) -> FetchResult:
    """Follow validated redirects through a supplied request callable.

    `request(url, headers) -> (status, headers, body)`. The callable owns TLS
    and sockets. This function owns policy, byte counting and auth stripping.
    """
    policy.check_root(url)
    policy.require_budget()
    if type(policy.max_redirects) is not int or policy.max_redirects < 0:
        raise PolicyError(
            "invalid_redirect_limit", "Redirect limit must be a non-negative integer"
        )
    current = url
    authorization_allowed = authorization is not None
    authorization_forwarded = False
    redirects = 0
    received = 0
    while True:
        headers = {}
        if authorization_allowed and authorization:
            headers["Authorization"] = authorization
            authorization_forwarded = authorization_forwarded or redirects > 0
        status, response_headers, body = request(current, headers)
        declared = _content_length(response_headers, current)
        received = policy.count_received(len(body), declared, already_received=received)
        location = _header(response_headers, "Location")
        if status in {301, 302, 303, 307, 308}:
            redirects += 1
            if redirects > policy.max_redirects:
                raise PolicyError("redirect_limit", "Too many redirects", current)
            target = policy.resolve_redirect(current, location)
            if origin_of(current) != origin_of(target):
                authorization_allowed = False
            current = target
            continue
        if status != 200:
            raise PolicyError("http_error", f"Unexpected status {status}", current)
        return FetchResult(
            url=redact_url(current),
            status=status,
            body=body,
            redirects=redirects,
            received=received,
            declared=declared,
            authorization_forwarded=authorization_forwarded,
        )


def _header(headers: dict[str, str], name: str) -> str | None:
    wanted = name.lower()
    for key, value in headers.items():
        if key.lower() == wanted:
            return value
    return None


def _content_length(headers: dict[str, str], url: str | None = None) -> int | None:
    raw = _header(headers, "Content-Length")
    if raw is None:
        return None
    value = raw.strip()
    if not value or not value.isascii() or not value.isdigit():
        raise PolicyError(
            "invalid_content_length",
            "Content-Length must be a non-negative decimal integer",
            url,
        )
    return int(value)
