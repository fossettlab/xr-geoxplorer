# Migration: restructure the `geoxplorer-source` Azure container

**Date:** 2026-06-16
**Container:** `haringerverdiag` / `geoxplorer-source` (the source-of-truth for the #6 re-bake)
**Goal:** one consistent, self-describing layout so code can be built against stable
paths and the `~`-suffix / naming-collision confusion is removed.
**Reversibility:** the complete pre-migration blob inventory is recorded in
[`geoxplorer-source-before.txt.gz`](geoxplorer-source-before.txt.gz) (38,938 blobs). Every
step below is a server-side prefix copy with an explicit inverse, so the container can
be returned to its exact prior state.

## Why

Pre-migration the container mixed three naming schemes and a semantic collision:
- 8 root folders carried Unity's `~` "do-not-import" suffix (`DEM~`, `HandSamples~`, …),
  meaningless in a blob store.
- `production-source/<category>/` (raw `.obj`/`.dae`, no `.meta`) and
  `recovered-source/<Project>/` (importable, with `.meta`) used different axes.
- `CrystalLattice~` holds crystal **structures** (bcc, diamond, nanotubes), but the
  deployed `geoxplorer-crystallattice` category is **minerals** (from `CrystalViewer`).
  Same word, two meanings.

## Target layout (REVISED 2026-06-16 after codex review)

The first proposal nested folders by project (`importable-source/<Project>/<category>`).
A codex review of the consumer (`Assets/Editor/GeoXAssetBundlePipeline.cs`, PR #82) showed
`FindCategoryPath` only matches a `<category>`/`<category>~` folder among the **immediate
children** of the source root — so project-nested folders would not be found. But
`EnumerateModelAssetPaths` recurses (`SearchOption.AllDirectories`) *within* a found category
folder, and bundle names are manifest/filename-based (not path-based). So the layout must be
**category-first at the top level, with provenance subfolders inside**:

```
geoxplorer-source/
  _README.md
  importable-source/                  point -geoXSourceRoot here
    archeology/   GeoXAssetBundles/...
    architecture/ GeoXAssetBundles/...
    arthistory/   GeoXAssetBundles/...
    crystallattice/ CrystalViewer/...           deployed crystallattice = MINERALS
    dem/          GeoXAssetBundles/... + LROAssetBundles/...   (0 name overlap)
    drama/        GeoXAssetBundles/...
    handsample/   MineralHandSamples/...         authoritative; covers all 53 deployed
    outcrop/      GeoXAssetBundles/...
    bio/          FossettLabDemo/...
  crystal-structures/   GeoXAssetBundles/...     old CrystalLattice~ (NOT deployed; parked)
  raw-source-no-meta/   crystallattice/ handsample/ dem/    production-* raw .dae/.obj
```

Dual-source decision (coverage check 2026-06-16): handsample -> MineralHandSamples only
(GeoXAssetBundles HandSamples~ is a 31-of-53 subset, contributes nothing unique, dropped);
dem -> GeoXAssetBundles DEM~ (5) + LROAssetBundles (6), zero name collision.

## Exact prefix mapping (old -> new)

| old prefix | new prefix |
|---|---|
| `Archeology~/`     | `importable-source/archeology/GeoXAssetBundles/` |
| `Architecture~/`   | `importable-source/architecture/GeoXAssetBundles/` |
| `ArtHistory~/`     | `importable-source/arthistory/GeoXAssetBundles/` |
| `Drama~/`          | `importable-source/drama/GeoXAssetBundles/` |
| `Outcrops~/`       | `importable-source/outcrop/GeoXAssetBundles/` |
| `DEM~/`            | `importable-source/dem/GeoXAssetBundles/` |
| `recovered-source/LROAssetBundles/`    | `importable-source/dem/LROAssetBundles/` |
| `recovered-source/CrystalViewer/`      | `importable-source/crystallattice/CrystalViewer/` |
| `recovered-source/MineralHandSamples/` | `importable-source/handsample/MineralHandSamples/` |
| `recovered-source/FossettLabDemo/`     | `importable-source/bio/FossettLabDemo/` |
| `CrystalLattice~/` | `crystal-structures/GeoXAssetBundles/` (parked; outside importable-source) |
| `production-source/crystallattice/` | `raw-source-no-meta/crystallattice/` *(done)* |
| `production-source/handsample/`     | `raw-source-no-meta/handsample/` *(done)* |
| `production-source/dem/`            | `raw-source-no-meta/dem/` *(done)* |
| `HandSamples~/` | (dropped — subset of MineralHandSamples; retained in before-snapshot only) |

## Procedure

1. **Discard** the earlier wrong project-nested `importable-source/` copy (it duplicated
   originals; deleting it removes no unique data).
2. **Copy** (server-side) per the table above into the category-first layout. Originals untouched.
3. **Verify** transformed blob-name set + sizes per new prefix (not just counts); confirm no
   stale blobs under new prefixes.
4. **Delete old prefixes** — GATED on a successful Unity `ValidateSourceCoverageAgainstManifest`
   run against a local mirror of the new `importable-source/` (requires Unity; handed to the
   contractor). Per codex: count parity is NOT sufficient; do not delete until Unity validation
   passes. Old prefixes remain until then.

## Unwind

- Before any delete: drop the new prefixes (`importable-source/`, `raw-source-no-meta/`)
  and the container is byte-identical to the snapshot.
- After delete: re-copy new->old using the inverse of the table above;
  `geoxplorer-source-before.txt.gz` (gunzip first) is the authoritative target state to verify against.

## Status

- [x] Before-inventory captured (38,938 blobs)
- [x] First copy attempt (project-nested) — superseded; codex review showed it breaks the pipeline
- [x] `raw-source-no-meta/` populated from `production-source/*` (correct in both layouts)
- [x] Discard wrong project-nested `importable-source/` (38,488 removed, 0 failed)
- [x] Re-copy into category-first layout — 0 failures; `importable-source/` = 38,023
      (immediate children = the 9 pipeline categories), `crystal-structures/` = 116
- [x] Verify — all 11 mapping pairs pass exact path+size parity (0 missing/extra/sizediff);
      no stale blobs
- [x] Upload `_README.md` to container root
- [x] Gate cleared — Sean confirmed on #82 (2026-06-18): rebased on `main` incl. #86;
      `ValidateSourceLayout` passed; `ValidateSourceCoverageAgainstManifest
      -geoXAllowPartialSource=true` passed (android 174 matched / 719 source-missing /
      17 bio skipped); available-source bake + output-against-manifest validation passed
- [x] Re-verified current-state parity for all 14 copied prefixes (0 miss/extra/sizediff);
      `HandSamples~` is the sole intentional drop (subset of MineralHandSamples, retained on NAS)
- [x] **Deleted old prefixes (2026-06-18) — 38,938 blobs removed, 0 failures.**
      Final container: `_README.md` + `importable-source/` (38,023) + `crystal-structures/`
      (116) + `raw-source-no-meta/` (450). Migration COMPLETE.

> Post-deletion unwind requires re-uploading from the NAS source projects (the in-container
> originals are gone); `geoxplorer-source-before.txt.gz` is the authoritative target listing.
