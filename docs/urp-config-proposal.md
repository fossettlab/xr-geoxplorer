# URP configuration proposal — Quest 3 (#13)

**Status: starting configuration approved (2026-07-27). Not yet implemented**
(blocked on Session 1). These are the agreed *starting* render settings for the
Built-in RP -> URP migration, following Meta/Unity guidance for Quest URP. Every
value is a starting point to validate and tune on-device against the Session 1
pre-URP baseline — none is a measured result. The values are ratified as the
starting point; on-device tuning in Session 2 may adjust them.

## Why URP, and what approving this commits to

Quest 3 wants URP for the mobile-VR fast path (single-pass instanced stereo,
foveated rendering, tiled-GPU-friendly rendering). Migration scope in this repo is
smaller than the ticket's "XXL" implies:

- **~23 app materials**, all on built-in Standard/Mobile shaders; **no custom
  hand-written shaders and no Shader Graphs.** Unity's automatic built-in -> URP
  material converter handles the bulk; a few may need manual touch-up.
- **MRTK materials are not migrated here** — they are replaced wholesale by MRTK3
  (#14/#15). So this ticket is mostly the URP asset config + the automated
  material convert + a perf-regression gate, not shader authoring.

## Proposed URP Asset (quality) settings

| Setting | Proposed | Rationale |
|---|---|---|
| HDR | **Off** | LDR is standard for Quest; HDR costs bandwidth on a tiled GPU |
| MSAA | **4x** | Quest has no post-AA; MSAA is the recommended edge AA. Drop to 2x if GPU-bound |
| Render Scale | **1.0** | Start at native; tune 0.9-1.2 on device; revisit with foveation |
| Rendering Path | **Forward** | Few lights in-scene; lighter than Forward+ |
| Depth Texture | **Off** | Enable only if an effect needs it (costs a depth prepass) |
| Opaque Texture | **Off** | Enable only for refraction/distortion (costs a copy) |
| Main Light | **Per-Pixel, shadows On** | One directional light with shadows |
| Shadow Resolution | **2048** | Balance; 1024 if shadow-map bandwidth bites |
| Shadow Distance | **~20 m** | Table-scale scene; short distance = cheap shadows |
| Shadow Cascades | **1** | Single cascade is standard for mobile VR |
| Soft Shadows | **Off** (or Low) | Expensive on Quest |
| Additional Lights | **Per-Pixel, max 4** (or Off) | Cap per-pixel lights; disable if scene is single-light |
| SRP Batcher | **On** | Draw-call batching for URP |
| Dynamic Batching | **Off** | Superseded by SRP Batcher |

## Proposed Universal Renderer settings

| Setting | Proposed | Rationale |
|---|---|---|
| Rendering Path | **Forward** | Matches the asset |
| Depth Priming | **Disabled** | Depth priming tends to hurt on mobile/tiled GPUs |
| Intermediate Texture | **Auto** | Never "Always" — forcing a blit kills the Quest fast path |
| Renderer Features | **none initially** | Add only if a specific effect requires it |
| Post-processing | **none / minimal** | Bloom/tonemapping are costly on Quest; avoid unless needed |

## Quest / OpenXR specifics

- **Stereo rendering: Single Pass Instanced** — already configured (OpenXR render
  mode + player stereo path). URP supports it; keep it.
- **Foveated rendering: enable (Vulkan Fixed Foveated Rendering).** Currently off
  in the Meta Quest feature; enabling it is a large Quest 3 perf win. Propose
  medium level; validate the peripheral-quality tradeoff on device.
- **Vulkan** graphics API and **Optimize Buffer Discards** — already set; keep.

## Validation (the perf-regression gate — needs the Quest)

After implementation, on Quest 3:

1. Confirm render correctness: no magenta materials, shadows/lighting look right,
   UI and globe/models render in stereo.
2. Re-run the Session 1 perf capture (FPS, frame time, GPU/CPU levels) with this
   URP config. **The gate: URP matches or beats the Built-in RP baseline** (the
   ticket's bar is no worse than a small regression). Tune the knobs above
   (render scale, MSAA, shadows, foveation) until it does.

## Sequencing

Implementing URP is gated on Session 1: #13 is blocked by #10 (build+deploy), and
the perf gate needs the Session 1 baseline to measure against. So: **Session 1
(first light + baseline) -> URP implementation (headless) -> Session 2 (URP
validation).** This proposal is the last purely-headless design step before the
headset becomes the critical path.
