# Meta Quest App Lab submission checklist (#33)

**Status:** planning checklist — most items need Mac + Quest hardware or Meta Developer
Dashboard access. Use this when the modernization critical path (#10, #13, #23, etc.)
is far enough along to produce a release candidate.

**Default channel:** App Lab (lighter review). Main Quest Store deferred.

## Compliance gates

- [ ] `targetSdkVersion = 34` — verify in final APK (`adb shell dumpsys package …`)
- [ ] ASTC texture compression on production textures
- [ ] APK signed with production keystore (not in repo)
- [ ] Application ID matches Meta developer dashboard
- [ ] `versionCode` monotonically increasing

Reference: [`docs/quest-android-store-settings.md`](quest-android-store-settings.md),
[`Assets/Plugins/Android/AndroidManifest.xml`](../Assets/Plugins/Android/AndroidManifest.xml).

## Permissions (already declared — verify justifications in store listing)

| Permission | Justification |
|---|---|
| `RECORD_AUDIO` | Real-time voice in collaborative sessions |
| `USE_ANCHOR_API` | Persistent spatial anchors for content placement |
| `USE_SCENE` | Room geometry for placement / occlusion |
| Hand tracking | Hand-based interaction with globe and models |

## Privacy + Meta DPDD

- [ ] Privacy policy at stable URL (covers voice transient use, anchor storage, no v1 analytics)
- [ ] Meta Data Protection Disclosure form completed per permission
- [ ] Coordinate with Fossett Lab / WashU policy reviewer

Draft outline: [`docs/privacy-policy-outline.md`](privacy-policy-outline.md) (when added).

## Crash reporting

- [ ] Ship without third-party analytics for v1 (document explicitly)
- [ ] Confirm Meta dashboard receives a deliberate dev-build crash

## Thermal + perf soak (Quest hardware)

- [ ] 30–45 min session: voice + sync + anchors + passthrough
- [ ] Capture OVR Metrics / MQDH throughout
- [ ] No sustained thermal throttle; frame budget holds
- [ ] If throttle occurs → document and consider #34 ASW spike

Baseline comparison: Session 1 runbook perf numbers from [`docs/quest-session-1-runbook.md`](quest-session-1-runbook.md).

## Store listing assets (produced outside repo)

- [ ] App icon (Meta required sizes)
- [ ] Hero / cover art
- [ ] 3–5 in-headset screenshots
- [ ] Short description (≤100 chars)
- [ ] Long description
- [ ] Category + age rating
- [ ] Pricing: free
- [ ] App Lab beta tester list

## Pre-submission

- [ ] Upload to App Lab via MQDH
- [ ] Internal review with project lead
- [ ] Submit for Meta review

## Blockers before this checklist matters

| Blocker | Issue |
|---|---|
| Quest build runs on hardware | #10 |
| URP perf acceptable | #13 |
| Networking rewrite or spike go | #22 / #23 |
| Firebase / anchor backend closed | #40 |
| Restricted container private + SAS | #37 |
| HW smoke suite | #32 |

## Docs

- [Meta publish overview](https://developers.meta.com/horizon/resources/publish-app-submission/)
- [App Lab](https://developers.meta.com/horizon/resources/publish-app-lab/)
- [Data Protection Disclosure](https://developers.meta.com/horizon/resources/publish-data-use/)
