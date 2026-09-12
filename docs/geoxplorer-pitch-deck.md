# GeoXplorer

*A geospatial and scientific spatial framework for XR.*

Short pitch deck · September 2026 · Product direction; capabilities in development.

---

## The dataset is here, and I can work with it

Bring terrain, imagery, observations and 3D reconstructions into one spatially
registered scene. Examine the study area on a table or explore it at immersive
scale, while preserving its coordinates, units and relationships.

Quest first. iOS later.

---

## Seeing a model is the beginning

Scientific work also needs to establish:

- Where does this belong, and what does its scale mean?
- How does it align with other observations?
- Where did it come from, and what changed during processing?
- Which conclusions does its evidence support?

GeoXplorer makes those questions part of the scene itself.

---

## What GeoXplorer owns

A durable scientific scene: layers, reference systems, registration, source
identity, provenance and uncertainty where known.

A quiet spatial interface: act directly on the dataset, inspect details when
needed, and keep scientific information accessible without filling the headset
with panels.

Earth, Moon, Mars and local scenes share the same architectural principles.
Unknown location or scale stays explicit.

---

## Where it sits

```text
Scientific data, models and generated environments
                        ↓
GeoXplorer: spatial meaning, registration and scientific interaction
                        ↓
Rendering and XR backends
```

GeoXplorer builds on existing formats and geodetic/rendering tools. Unity/OpenXR
is the first runtime; Three.js/WebXR is a future portability target. Meshes,
point clouds, tiles and splats can provide different representations of a layer.

The scene should remain scientifically meaningful when its renderer changes.

---

## The distinctive proposition

**Scientific continuity through spatial interaction.**

Combine referenced observations with an explicitly registered reconstruction.
Move and scale the scene without changing the science. Exchange a representation
without losing source identity. Save the scene so another conforming runtime
can recover its meaning.

That combination is the product's intended distinction. It is a testable design
promise, not a claim that no other tool offers overlapping capabilities.

---

## A concrete research and teaching workflow

Place a study area on a table. Reveal terrain, imagery and sample locations.
Select an observation to inspect its record. Register an unreferenced outcrop
model through an explicit, documented operation. Compare the layers, inspect
uncertainty, and share the same scientific scene with a collaborator.

The same pattern can support a terrestrial field site, a planetary surface
study or a specimen in a local frame.

---

## New representations strengthen the framework

Gaussian splats and generative systems can supply new views and hypotheses.
Their outputs enter through adapters, retaining registration and provenance.

Marble already documents splat and GLB exports; a future GeoXplorer trial can
start with those files. World Labs Atlas points toward richer reconstruction
and generated environments, including explicit 3D outputs. Its early-access
integration remains to be qualified. GeoXplorer's role is to preserve scientific
context and distinguish observations from inferred content.

Sources: [Marble exports](https://docs.worldlabs.ai/marble/export/specs),
[World Labs Atlas](https://www.worldlabs.ai/blog/atlas).

---

## Build the scientific spine, prove it on Quest

Quest/OpenXR baseline → portable scene contract → qualified CRS operations →
coordinate separation → explicit registration and evidence → additional adapters.

**Today:** a Unity-independent Cartesian scene core and invariant tests exist
locally. Quest modernization is in progress.

**Next:** prove a registered scientific scene on Quest and its portable meaning
with an independent reader. Full geodesy, registration tools, additional
renderers and generative integrations remain implementation work.

The near-term deliverable is a reproducible scientific workflow that a researcher
or student can open, understand and use.
