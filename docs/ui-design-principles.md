# GeoXplorer UI design principles

**Product design direction supplied by the user, 2026-09-04.** This document
captures the UI addendum for implementation and review. It complements the
[scientific scene architecture](open-3d-plan.md) and
[Beads execution plan](beads-execution-plan.md). It records intended behavior,
not an implemented or device-validated interface.

## Core principle

> Modern spatial-computing UI over a rigorous scientific spatial-data model.

GeoXplorer provides sophisticated scientific and geospatial capability without
inheriting desktop GIS complexity or visual conventions. “GIS-like UI” is not a
design target. The dataset usually dominates the field of view; interface
elements appear contextually and recede when they are no longer needed.

The intended first impression is: **“The dataset is here in front of me, and I
can work with it.”** Quest is the first design and acceptance target; iOS follows.

## Interaction and visual direction

Use direct manipulation, progressive disclosure, strong typography, generous
spacing, simple geometry, subtle depth, clear hierarchy, restrained transitions,
and consistent interaction states. Keep text legible at actual headset viewing
distances and controls comfortable to reach. Determine their dimensions through
prototype and device review; this document invents no ergonomic thresholds.

Avoid permanent toolbars, large layer trees, dense property inspectors, rows of
small icons, nested modal dialogs, game-like menus, decorative 3D controls,
skeuomorphic scientific instruments, and panels floating unnecessarily around
the user. Preserve useful functions while removing redundant screens or buttons.

| Natural action | Intended result |
|---|---|
| Grab the dataset | Move the scene display root. |
| Use a two-hand gesture | Change display scale for the whole registered scene. |
| Select a feature | Inspect it contextually. |
| Select two points with an available measurement tool | Measure using the explicitly defined scientific method. |
| Select a layer | Reveal that layer's controls. |

Provide discoverable alternatives where a gesture is unavailable or unsuitable.
Do not add a persistent button when direct interaction makes the action clearer.

## Progressive disclosure

The resting view exposes very little UI. A compact, contextual group such as
**Layers · Inspect · Measure**, with a meaningful scale readout, illustrates the
hierarchy; it is not a required permanent toolbar. Show Measure only when a
working, scientifically defined tool is available.

Opening Layers reveals a simple list with visibility controls. Selecting a layer
reveals useful secondary actions such as opacity, legend, metadata, and
provenance. Advanced detail exposes CRS definitions, source transformations,
processing history, and technical identifiers. Dismissing these controls returns
attention to the dataset. Opening, loading, cancellation, retry, and sharing
follow the same restrained visual language.

## Hide complexity, preserve scientific information

Manage coordinates, registration, units, transformations, LOD, and provenance
automatically when supported metadata and methods permit. All remain inspectable.
UI simplification must not weaken the scientific representation or silently infer
missing information.

Body/reference context may be Earth, Moon, Mars, another explicitly described
body, or a local/unknown frame. Never default planetary or unreferenced content
to Earth/WGS84. Detailed frame definitions belong in Advanced; units, unknown
calibration, frame conflicts, and active exaggeration belong alongside the
readout when they change its interpretation. A latitude/longitude readout must
use the declared body and convention. Local XYZ is valid where appropriate.

Present distance, display scale, exaggeration, and location as clear, restrained
values with units and context. Precision must follow the data and method.
The numerical examples in the supplied design prompt illustrate formatting;
they are not measured results, default values, or a requirement to invent
coordinates. Use a scientific scale ratio or “near 1:1” only when a physical
scale basis is available from declared units/mappings or calibration. Keep its
source-declared versus validated status inspectable. Unknown units permit zoom
and resizing without a physical ratio; independent calibration is not a viewing
prerequisite. See the [reviewed contract](scientific-scene-contract.md).

## Tabletop and immersive continuity

Tabletop is a primary interaction mode. The dataset should feel physically
present on the table, with contextual UI near it or at an ergonomically suitable
location. Manipulate the whole registered scene without disturbing its layers.

Allow transition to immersive or near-1:1 exploration where appropriate. The
scientific scene remains the same: change display representation and viewpoint
while preserving registration, layer identity, visibility, selection, and source
state. Returning to the tabletop should be easy and retain orientation. Keep
personal viewpoint changes distinct from shared scene placement.

## Design acceptance

For every proposed UI element, record the answers to the user's design test:

1. Does this need to be permanently visible?
2. Can the user act directly on the data instead?
3. Is this a common operation or an advanced operation?
4. Can GeoXplorer infer or manage this automatically?
5. Does exposing this improve scientific understanding?
6. Would the interface make sense to a scientist who has never used GIS software?

Hide or remove controls when these answers show they are unnecessary. Review
mockups for resting, opening, selected-layer, inspection, measurement, advanced,
error, tabletop, and immersive states. Validate readability, reach, selection,
dismissal, recovery, and transitions in the actual Quest build. Screenshots or
mockups establish design intent; they do not establish headset usability.
