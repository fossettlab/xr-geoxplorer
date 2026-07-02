#!/usr/bin/env python3
"""Upload GeoXplorer AssetBundle staging files from azure-upload-plan.json.

The Unity editor pipeline writes an upload plan after a local bake. This helper
consumes that plan and uploads each listed bundle to a staging Azure Blob
container while applying the custom metadata captured from the deployed
manifest.
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import mimetypes
import posixpath
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Any, Dict, Iterable


AZURE_API_VERSION = "2023-11-03"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Upload GeoXplorer AssetBundles to an Azure staging container."
    )
    parser.add_argument(
        "--plan",
        required=True,
        help="Path to azure-upload-plan.json written by GeoXAssetBundlePipeline.",
    )
    parser.add_argument(
        "--container-url",
        required=True,
        help=(
            "Azure Blob container URL including SAS query string, for example "
            "https://account.blob.core.windows.net/staging-assetbundles?<sas>"
        ),
    )
    parser.add_argument(
        "--mode",
        choices=("staging", "production-shape"),
        default="staging",
        help=(
            "staging uploads to each entry's stagingBlobName in one container. "
            "production-shape uploads to targetBlobName and expects the supplied "
            "container URL to be the matching platform container."
        ),
    )
    parser.add_argument(
        "--platform",
        action="append",
        dest="platforms",
        help="Limit upload to one platform. Can be passed more than once.",
    )
    parser.add_argument(
        "--limit",
        type=int,
        default=0,
        help="Upload at most N blobs. Useful for smoke tests.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the planned uploads without sending requests.",
    )
    return parser.parse_args()


def load_plan(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        plan = json.load(handle)

    if plan.get("schemaVersion") != 1:
        raise ValueError(f"Unsupported upload plan schemaVersion: {plan.get('schemaVersion')}")
    if not isinstance(plan.get("containers"), dict):
        raise ValueError("Upload plan is missing a containers object.")
    return plan


def iter_entries(plan: Dict[str, Any], platforms: Iterable[str] | None) -> Iterable[Dict[str, Any]]:
    platform_filter = {platform.lower() for platform in platforms or []}
    containers = plan["containers"]
    for platform, entries in containers.items():
        if platform_filter and platform.lower() not in platform_filter:
            continue
        if not isinstance(entries, list):
            raise ValueError(f"Upload plan platform '{platform}' is not a list.")
        for entry in entries:
            yield entry


def validate_blob_name(blob_name: str) -> str:
    normalized = posixpath.normpath(blob_name).replace("\\", "/")
    if (
        not blob_name
        or blob_name.startswith("/")
        or normalized == "."
        or normalized.startswith("../")
        or "/../" in normalized
    ):
        raise ValueError(f"Unsafe blob name in upload plan: {blob_name!r}")
    return blob_name.strip("/")


def build_blob_url(container_url: str, blob_name: str) -> str:
    parsed = urllib.parse.urlsplit(container_url)
    if not parsed.scheme or not parsed.netloc:
        raise ValueError("--container-url must be an absolute Azure container URL.")

    encoded_blob = "/".join(urllib.parse.quote(part, safe="") for part in blob_name.split("/"))
    base_path = parsed.path.rstrip("/")
    path = f"{base_path}/{encoded_blob}"
    return urllib.parse.urlunsplit((parsed.scheme, parsed.netloc, path, parsed.query, ""))


# Azure blob metadata names must be identifier-like (letters, digits,
# underscores; not starting with a digit). Values become HTTP header values, so
# they must be ASCII with no control characters.
_METADATA_NAME_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")


def metadata_headers(metadata: Dict[str, Any]) -> Dict[str, str]:
    headers: Dict[str, str] = {}
    for key, value in sorted(metadata.items()):
        if value is None:
            continue
        normalized_key = str(key).strip()
        if not normalized_key:
            continue
        # Fail fast on invalid names/values rather than sending a malformed or
        # injected HTTP header, or silently mangling the manifest metadata.
        if not _METADATA_NAME_RE.match(normalized_key):
            raise ValueError(
                f"Invalid Azure metadata name {normalized_key!r}: use letters, "
                "digits, and underscores, not starting with a digit."
            )
        text = str(value)
        if any(ord(ch) < 0x20 or ord(ch) == 0x7f for ch in text):
            raise ValueError(
                f"Metadata {normalized_key!r} contains control characters; "
                "refusing to send an unsafe HTTP header."
            )
        try:
            text.encode("ascii")
        except UnicodeEncodeError:
            raise ValueError(
                f"Metadata {normalized_key!r} has a non-ASCII value; Azure blob "
                "metadata values must be ASCII."
            )
        headers[f"x-ms-meta-{normalized_key}"] = text
    return headers


def upload_blob(blob_url: str, source_path: Path, content_type: str, metadata: Dict[str, Any]) -> None:
    size = source_path.stat().st_size
    headers = {
        "Content-Length": str(size),
        "x-ms-version": AZURE_API_VERSION,
        "x-ms-date": dt.datetime.now(dt.timezone.utc).strftime("%a, %d %b %Y %H:%M:%S GMT"),
        "x-ms-blob-type": "BlockBlob",
        "x-ms-blob-content-type": content_type,
    }
    headers.update(metadata_headers(metadata))

    with source_path.open("rb") as body:
        request = urllib.request.Request(blob_url, data=body, headers=headers, method="PUT")
        with urllib.request.urlopen(request) as response:
            if response.status not in (200, 201):
                raise RuntimeError(f"Unexpected Azure response {response.status} for {blob_url}")


def main() -> int:
    args = parse_args()
    plan = load_plan(Path(args.plan))
    entries = list(iter_entries(plan, args.platforms))
    if args.limit > 0:
        entries = entries[: args.limit]

    if not entries:
        print("No upload-plan entries matched the requested filters.", file=sys.stderr)
        return 1

    uploaded = 0
    for entry in entries:
        source_path = Path(entry["sourcePath"])
        if not source_path.is_file():
            raise FileNotFoundError(f"Source bundle listed in upload plan was not found: {source_path}")

        blob_name_key = "stagingBlobName" if args.mode == "staging" else "targetBlobName"
        blob_name = validate_blob_name(str(entry[blob_name_key]))
        content_type = str(
            entry.get("contentType")
            or mimetypes.guess_type(source_path.name)[0]
            or "application/octet-stream"
        )
        metadata = entry.get("metadata") or {}
        if not isinstance(metadata, dict):
            raise ValueError(f"Upload-plan metadata for {source_path} is not an object.")

        blob_url = build_blob_url(args.container_url, blob_name)
        print(f"{'DRY RUN ' if args.dry_run else ''}upload {source_path} -> {blob_name}")
        if not args.dry_run:
            upload_blob(blob_url, source_path, content_type, metadata)
        uploaded += 1

    print(f"{'Planned' if args.dry_run else 'Uploaded'} {uploaded} AssetBundle blob(s).")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (FileNotFoundError, ValueError) as exc:
        print(f"AssetBundle upload plan error: {exc}", file=sys.stderr)
        raise SystemExit(1)
    except urllib.error.HTTPError as exc:
        print(f"Azure upload failed: HTTP {exc.code} {exc.reason}", file=sys.stderr)
        print(exc.read().decode("utf-8", errors="replace"), file=sys.stderr)
        raise SystemExit(1)
