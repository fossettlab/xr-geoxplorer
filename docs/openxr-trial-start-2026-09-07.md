# OpenXR trial startup checkpoint

**Historical startup record.** The user subsequently approved the stalled
licensing-client restart. Recovery succeeded and OpenXR 1.17.0 passed all 51
Edit Mode tests. The package and reviewed Android repair are now integrated;
see [current qualification evidence](openxr-qualification-2026-09-07.md).

The user resumed mechanical execution after the review pause. The active goal
follows `geox-k1v.6` then `.5`: qualify OpenXR, apply the reviewed Android repair
after qualification, and produce a tested Android build checkpoint. Quest device
acceptance stays separate. No commit, push or publication is authorized by this
checkpoint.

## Prepared

An isolated copy lives at `/private/tmp/geox-openxr-117-9ua8dxr3`. It contains the
current Assets, Packages, ProjectSettings and docs, including uncommitted repairs
and tests. `trial-source-manifest.json` records the source branch/HEAD and hashes
of 7,842 copied files. The integration checkout remains on
`codex/scientific-scene-foundation`; its existing dirty work is retained.

Unity CLI identifies installed Unity 6000.4.4f1 with Android SDK/NDK and OpenJDK.
The trial still has OpenXR 1.16.1. The reviewed 1.17.0 package has not been installed.

## Startup blocker

Automated Editor launches and an attached automated batch launch did not reach
project initialization or a reachable Pipeline server. The launch log reports
licensing not initialized; the licensing-client log reports inability to acquire
the `Unity-LicenseClient-abradley` mutex. A separate CLI license-status check
outside the sandbox reports active licensing and a signed-in account. These
observations indicate a startup/IPC problem, not an OpenXR qualification result.

Automatic approval review rejected stopping licensing client PID 37235 because
its ownership and impact on other sessions were not established. The hung trial
Editor PID 46855 was separately identified by its exact project path and stopped
with SIGTERM. Existing Hub and licensing clients were left untouched.

`geox-k1v.6` is blocked pending explicit permission for the licensing-client
recovery. The dependent Android repair stays gated. There is no new compilation,
test, Android build or Quest result.

## Resume

With permission, recheck process identity before touching the stalled licensing
client; let the official Unity launch workflow restart it. Do not terminate Hub's
separate client. Never print raw Editor process arguments: Unity CLI injects launch
credentials. Trial launch logs were sanitized and are not publication artifacts.

Once Pipeline is reachable, discover registered package/settings/test commands,
capture effective baseline settings, and follow
[the reviewed trial](runtime-package-review-2026-09-07.md). Do not bypass the
package qualification gate just because the isolated source snapshot exists.
