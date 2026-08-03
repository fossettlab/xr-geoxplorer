# Quest lab organization — naming clarification

When discussing lab Quest headsets or Meta access, use these roles consistently:

## Managed organization

Controls **Q01**, **Q02**, and **Q03** through **Admin Center** and **Shared Mode**.

This is the IT/lab management layer: device enrollment, org-wide policies, and
shared-headset operation across all three units.

## Developer team

Owns the **xr-geoxplorer** app: builds, releases, and **developer access for Q01**.

This is the app-development layer. Developer Mode, APK sideloading, and store
submission work for the primary dev headset (Q01) live here—not in the managed
org's day-to-day Shared Mode workflow.

## Quick reference

| Role | Scope | Typical tools |
|------|-------|---------------|
| Managed organization | Q01, Q02, Q03 | Admin Center, Shared Mode |
| Developer team | App + Q01 dev access | Unity, MQDH, `adb`, Meta Developer org |
