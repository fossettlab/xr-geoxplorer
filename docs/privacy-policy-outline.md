# Privacy policy outline (draft for #33 — not legal advice)

Publish at a stable URL before App Lab submission. Have Fossett Lab / WashU legal
review before going live.

## Data we collect

- **Voice (Vivox / legacy Photon Voice):** processed in real time for multiplayer
  sessions; not recorded or stored by Fossett Lab servers for v1.
- **Spatial anchors:** anchor names and cloud anchor identifiers stored via backend
  (Firebase today; Azure Function per #40). Used to resume shared sessions.
- **Usage analytics:** none for v1.

## Data we do not collect

- No advertising IDs
- No sale of personal data
- No third-party analytics SDKs in v1 builds

## Permissions (map to Meta DPDD)

| App permission | Why |
|---|---|
| Microphone | Voice chat between session participants |
| Spatial anchors | Place geographic content in the user's space |
| Scene / room data | Detect surfaces for content placement |
| Hand tracking | Natural hand interaction |

## Storage and retention

- Anchor records: stored until expiration date on each anchor entry (typically 24h
  for legacy ASA; Meta anchors TBD in #17).
- Azure blob content (3D models): public-read educational assets; no user PII in blobs.

## Contact

- Fossett Laboratory, Washington University in St. Louis
- [Insert contact email before publication]

## Changes

- Last updated: [date]
- Policy version: 1.0-draft
