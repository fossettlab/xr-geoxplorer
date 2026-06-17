#!/usr/bin/env python3
"""Map every deployed AssetBundle to its surviving source (if any).

Inputs:
  docs/assetbundle-metadata-manifest.json   deployed catalog (android list is canonical)
  <index>                                   one line per source model file:
                                            "<basename_without_ext>|||<projectName>"
  Regenerate <index> on the NAS host with:
    for p in /mnt/nas/dev/fossett_xr_apps/*/; do
      proj=$(basename "$p"); [ -d "${p}Assets" ] || continue
      find "${p}Assets" -type f \\( -iname '*.prefab' -o -iname '*.fbx' \\
        -o -iname '*.obj' -o -iname '*.blend' -o -iname '*.dae' \\) \\
        -not -path '*/AssetBundles/*' | while read f; do
          bn=$(basename "$f"); echo "${bn%.*}|||$proj"; done
    done | sort -u

Output: docs/assetbundle-source-mapping.csv
  columns: bundle, category, prefabName, source_project, match_method, confidence

Matching tiers (most→least confident); first hit wins:
  exact      normalized names equal
  substring  one normalized name contains the other (len>=6 guard) — catches
             source names that append a formula/qualifier ("Anhydrite - CaSO4")
  tokenset   same set of word/number tokens (camelCase + digit boundaries split)
  fuzzy      difflib ratio >= 0.90 — typos/reorders; emitted as confidence=verify
  (none)     LOST
"""
from __future__ import annotations
import json, re, csv, sys
from pathlib import Path
from difflib import SequenceMatcher

ROOT = Path(__file__).resolve().parent.parent
MANIFEST = ROOT / "docs" / "assetbundle-metadata-manifest.json"
OUT = ROOT / "docs" / "assetbundle-source-mapping.csv"
INDEX = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("/tmp/nas_model_index.txt")

FUZZY_RATIO = 0.90
SUBSTR_MIN = 6

# prefabName values that are placeholders, not model identities — fall back to
# the bundle name stem for these (see manifest data-entry artifacts).
GENERIC_PREFABS = {"modelname", ""}

# Human-verified equivalences that automated matching cannot bridge (token glue
# vs. spacing). Keys are normalized identities; verified 2026-06-16 by direct
# presence in the staged source noted in the value.
ALIASES = {
    "monolake2000": "geoxplorer-source: importable-source/dem (LROAssetBundles/2000MonoLake)",
    "monolake2015": "geoxplorer-source: importable-source/dem (LROAssetBundles/2015MonoLake)",
}


def norm(s: str) -> str:
    s = s.lower().strip()
    s = re.sub(r"\.(prefab|fbx|obj|blend|dae|img)$", "", s)  # repeat-strip stacked exts
    s = re.sub(r"\.(prefab|fbx|obj|blend|dae|img)$", "", s)
    s = re.sub(r"(und|_und)$", "", s)
    s = re.sub(r"[-_ ]?bundle$", "", s)
    return re.sub(r"[^a-z0-9]", "", s)


def tokens(s: str) -> frozenset[str]:
    s = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", s)          # split camelCase first
    s = re.sub(r"(?<=[A-Za-z])(?=[0-9])|(?<=[0-9])(?=[A-Za-z])", " ", s)
    return frozenset(t for t in re.split(r"[^A-Za-z0-9]+", s.lower()) if t)


def load_index(path: Path):
    rows = []
    for line in path.read_text().splitlines():
        if "|||" not in line:
            continue
        bn, proj = line.rsplit("|||", 1)
        rows.append((bn, proj, norm(bn), tokens(bn)))
    return rows


def best_source(identity: str, idx) -> tuple[str | None, str, str]:
    """Return (project, method, confidence) or (None, 'none', '')."""
    n = norm(identity)
    tk = tokens(identity)
    if not n:
        return None, "none", ""
    for _, proj, sn, _ in idx:                           # exact
        if sn == n:
            return proj, "exact", "firm"
    for _, proj, sn, _ in idx:                           # substring (length-guarded)
        if len(n) >= SUBSTR_MIN and (n in sn or (len(sn) >= SUBSTR_MIN and sn in n)):
            return proj, "substring", "firm"
    for _, proj, _, st in idx:                           # token set
        if tk and tk == st:
            return proj, "tokenset", "firm"
    best, bestproj = 0.0, None                           # fuzzy (typos/reorders)
    for _, proj, sn, _ in idx:
        r = SequenceMatcher(None, n, sn).ratio()
        if r > best:
            best, bestproj = r, proj
    if best >= FUZZY_RATIO:
        return bestproj, "fuzzy", "verify"
    return None, "none", ""


def main():
    manifest = json.loads(MANIFEST.read_text())
    bundles = manifest["containers"]["android"]
    idx = load_index(INDEX)

    rows, tally = [], {}
    for b in bundles:
        cat = b["name"].split("/")[0].replace("geoxplorer-", "")
        prefab = b.get("metadata", {}).get("prefabName", "")
        # placeholder prefabName -> use the bundle name stem as the identity
        stem = b["name"].split("/", 1)[-1]
        stem = re.sub(r"-bundle$", "", stem)
        identity = stem if norm(prefab) in GENERIC_PREFABS else prefab

        if norm(identity) in ALIASES:
            loc, method, conf = ALIASES[norm(identity)], "verified", "firm"
            proj = "alias"
        else:
            proj, method, conf = best_source(identity, idx)
            if proj is None:
                loc = "LOST (no source found)"
            elif proj == "GeoXAssetBundles":
                loc = "geoxplorer-source (GeoXAssetBundles)"
            else:
                loc = f"NAS: {proj}"
        rows.append([b["name"], cat, prefab, loc, method, conf])
        key = "lost" if proj is None else ("verify" if conf == "verify" else "matched")
        tally[key] = tally.get(key, 0) + 1

    with OUT.open("w", newline="") as f:
        w = csv.writer(f)
        w.writerow(["bundle", "category", "prefabName", "source_location", "match_method", "confidence"])
        w.writerows(rows)

    print(f"deployed bundles: {len(rows)}")
    print(f"  matched (firm):  {tally.get('matched', 0)}")
    print(f"  matched (verify):{tally.get('verify', 0)}")
    print(f"  LOST:            {tally.get('lost', 0)}")


if __name__ == "__main__":
    main()
