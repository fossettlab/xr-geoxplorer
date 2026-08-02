# Performance baseline capture template (#11)

**Purpose:** standardized form for Quest Session 1 ([`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md))
and the #13 URP regression gate. Fill one row per capture session; commit the
CSV alongside PR notes or in `docs/perf-baselines/`.

## When to capture

| Milestone | Pipeline | Why |
|---|---|---|
| Session 1 (Built-in RP) | Built-in | Pre-URP reference — **required before #13** |
| Post-URP migration | URP | Regression gate vs Built-in baseline |
| Post-MRTK3 (#14–#16) | URP | Interaction layer cost |
| Pre-store (#33) | URP | App Lab submission evidence |

## Capture procedure (Quest 3)

1. Build release APK from committed project (Built-in RP for Session 1).
2. Enable metrics HUD (OVR Metrics Tool or MQDH performance panel).
3. Stand in a **fixed viewpoint** in `GeoXShared.unity` — record which scene area
   (e.g. "globe default zoom", "outcrop model loaded").
4. Record **30–60 seconds** steady-state (no menu navigation during sample).
5. Note refresh target (72 Hz default; 90 Hz if forced).
6. Fill the table below; attach screenshot of HUD if available.

## Results table

Copy [`docs/perf-baseline-template.csv`](perf-baseline-template.csv) or paste:

| Field | Example | Notes |
|---|---|---|
| `session_date` | 2026-08-02 | ISO date |
| `git_sha` | `f3bcbe9` | `git rev-parse --short HEAD` |
| `pipeline` | `builtin` or `urp` | Render pipeline |
| `scene_view` | globe-default | Fixed viewpoint label |
| `refresh_target_hz` | 72 | Device refresh setting |
| `fps_avg` | 72.0 | Average FPS over sample window |
| `fps_min` | 68 | Worst 1s average |
| `dropped_frames_pct` | 2.1 | Stale/dropped frame % if available |
| `cpu_ms_avg` | 8.2 | CPU frame time |
| `gpu_ms_avg` | 11.5 | GPU frame time |
| `cpu_level` | 2 | Fixed CPU clock level |
| `gpu_level` | 3 | Fixed GPU clock level |
| `thermal_state` | normal | normal / warning / throttled |
| `notes` | magenta on terrain | Free text |

## Pass criteria (#13 URP gate)

URP migration (#13) **must match or beat** the Built-in RP Session 1 baseline on
the same `scene_view` and refresh target:

- `fps_avg` ≥ baseline `fps_avg`
- `fps_min` ≥ baseline `fps_min` − 2 (small tolerance)
- `cpu_ms_avg` ≤ baseline + 1 ms
- `gpu_ms_avg` ≤ baseline + 1 ms

If the gate fails, investigate shader variants and draw calls before merging URP.

## Storage convention

Save completed captures as:

```text
docs/perf-baselines/YYYY-MM-DD-<pipeline>-<scene_view>.csv
```

Do not commit device serial numbers or user identifiers.

## Related

- [#11](https://github.com/fossettlab/xr-geoxplorer/issues/11) — perf baseline ticket
- [#13](https://github.com/fossettlab/xr-geoxplorer/issues/13) — URP migration
- [`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md) — Session 1 steps
