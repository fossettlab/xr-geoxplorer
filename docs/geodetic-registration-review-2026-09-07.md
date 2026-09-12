# Geodetic operations, registration and evidence review

Engineering policy reviewed on 2026-09-07. PROJ is the first provider to qualify;
Android support and production scientific transforms remain unqualified.
Registration work starts with declared mappings, extending the previously accepted
supplied Cartesian contract. No fitting, georeferencing inference, statistical
error model or automatic uncertainty propagation is approved by this review.

## Geodetic boundary: adopt a provider, preserve the operation

Use PROJ behind one evaluator boundary; do not build a projection engine or expose
PROJ handles through `GeoX.Spatial`. Persist original source/target definitions
and a complete selected operation with versioned resources. Another backend must
evaluate that meaning or explicitly report unavailable support. It must not repeat
an unconstrained operation search and silently choose a different result.

PROJ provides CRS/operation construction, axis-sensitive evaluation and controls
for approximate transformations and resource availability. Its APIs distinguish
operation discovery from evaluation. These capabilities justify the candidate;
they do not select a scientifically appropriate operation for a dataset.
[PROJ operation APIs](https://proj.org/en/stable/development/reference/functions.html).

Start qualification with grid-free, same-body geodetic/Cartesian conversions and
explicitly defined projections. The first desk probe is the published GRS80
geodetic-to-Cartesian example. Extend trials to sourced planetary definitions;
do not make Earth ECEF the universal intermediate. Data requiring missing height,
vertical datum, epoch, grid or an unsupported operation stays preserved and
unavailable for that conversion. Offline opening and local previews still work.

| Concern | Required behavior |
|---|---|
| Axis order / units | Respect full declared CRS axes at the scientific boundary. Any visualization normalization is an explicit, tested boundary conversion. Never assume longitude/latitude from a numeric tuple. |
| Height / vertical reference | Do not turn a 2D CRS into a 3D position by appending zero. Require explicit height and vertical meaning when the operation needs them. Horizontal-only results must stay identified as such. |
| Epoch / dynamic frames | Preserve supplied epochs; require them where the operation needs them. No present-day epoch default or cross-body ephemeris operation. |
| Body / planetary conventions | Require matching known bodies for geodetic conversion. Preserve planetocentric/planetographic latitude, longitude direction/domain, shape and vertical definitions; never infer them from Moon/Mars names. |
| Selection / domain | Record selected operation and area/domain of validity. Reject out-of-domain/non-finite results. No ballpark fallback; request `ALLOW_BALLPARK=NO` and `ONLY_BEST=YES` where the qualified discovery API supports them, then inspect the selected operation/resources. Flags alone are insufficient. |
| Reproducibility / offline | Pin provider and database versions/hashes, record required grid identities/hashes/licenses, disable automatic network downloads, and fail unavailable resources explicitly. No implicit use of a developer's cached grids. |
| Inverse / geometry | Report inverse availability; do not numerically invent one. Nonlinear geometry conversion, densification and local approximation need a recorded adapter method and its own evidence. |
| Accuracy | Provider operation accuracy, numerical tolerance and dataset accuracy are distinct. Unknown or absent accuracy stays unknown. Round-trip agreement alone is not independent scientific validation. |

Store standard CRS definitions using WKT2 or
[PROJJSON](https://proj.org/en/stable/specifications/projjson.html). The provider
trial must test the exact accepted encodings, dimension checks and axis policies.
Opaque definitions can round-trip without becoming executable. Source-supplied
operations are data until checked against the qualified operation/resource set;
they cannot open arbitrary files or trigger grid downloads.

## Evidence obtained during this review

The installed Homebrew PROJ reports **9.8.0**. A first search-path read exposed
an inherited Anaconda data path. The recorded probe therefore explicitly selects
the Homebrew database and disables network resources. Mixing a binary and an
ambient database is not acceptable qualification evidence.

The commands, database metadata/hash and raw output are preserved in
[the desktop receipt](contracts/proj-desktop-review-2026-09-07.json).
For reproducibility, the receipt includes argv, stdin and the explicit environment;
run each command with those values against the recorded provider/database.
No user dataset was transformed. This is a desktop reference probe only.

The published example supplies longitude `17.7562015132`, latitude `45.3935192042`,
height `133.12` and epoch `2017.8`. Running `cct -d 4 +proj=cart +ellps=GRS80`
returns `4272922.1553 1368283.0597 4518261.3501 2017.8000`, matching the published
rounded output. Explicit GRS80 replaces the example's implicit default; matching
the published decimal precision is a regression check, not a physical accuracy
bound. [PROJ reference example](https://proj.org/en/stable/operations/conversions/cart.html).

The same database resolves `IAU_2015:30100` and `IAU_2015:49900` to explicit Moon
and Mars spherical geographic definitions, preserved in the receipt. These are
2D definitions and are not proof of a height transformation or planetary
registration. USGS documents planetary WKT/PROJJSON definitions and custom
projections, including the need to state the actual CRS.
[USGS planetary CRS examples](https://psdi.astrogeology.usgs.gov/moon/standards/data_examples/).

## Remaining provider qualification: a mechanical spike

`geox-k1v.7.1` remains open for the empirical part. Record an exact native provider
build, database/resource bundle, license notices, Android ARM64/IL2CPP invocation,
managed/native ownership, error/lifecycle behavior and offline execution. Reproduce
the sourced Earth vector and sourced Moon/Mars cases, including negative axis,
missing-height, mismatched-body and unavailable-resource cases. Compare against
independent published or analytic references, not two wrappers around the same
library. State the precision of every expected result and why its test tolerance
is numerical, not a scientific acceptance threshold.

Return to higher-effort review if Android packaging is impractical, if the proposed
subset needs datum grids/dynamic operations, or if a dataset requires choosing
among scientifically different operations. A desktop CLI does not establish
Quest support. A server-only converter would also change the offline product
boundary and requires review; do not silently add a conversion service.

## Registration: record a declaration before adding estimation

The current increment accepts an externally supplied affine map or a user's
deliberate declaration of placement/scale against a named target frame. This
records what the user supplied; it does not infer a source CRS, solve control
points or claim accuracy. The accepted Cartesian math remains the implementation
basis. Preserve original unknown source body, axes and units even when a solution
relates its coordinates to a known target. Reject known conflicting bodies in
this initial workflow. No implicit conversion from lunar data into an Earth scene.

The UI provides a separate contextual **Register** action for a selected layer:

1. Select the target scene frame and either supply a mapping or deliberately edit
   a declared placement. Show the original source's unknown calibration and the
   target frame/units. Do not expose raw CRS machinery in the normal opening flow.
2. Preview only the selected layer's candidate registration; keep its previous
   solution and the rest of the registered scene intact. Ordinary grab/two-hand
   gestures outside this action still move/scale the whole displayed scene.
3. Show the declaration basis and unresolved units/evidence before Apply. If scale
   is established by the entered mapping, label it user-declared. If physical scale
   remains unknown, retain that status and suppress physical readouts.
4. Apply creates a new immutable operation/solution and scene revision, linked to
   the previous solution. Cancel discards preview. Undo restores the prior active
   solution (possibly null) through a new scene revision; it does not erase history.
   Reapplying without changes need not create a duplicate solution.

Allow ordinary preview with no registration. Do not require a numerical accuracy
field to open, preview or declare placement. Arbitrary supplied affine transforms
may include shear/reflection; inspect them faithfully. Direct controls need only
express placement, rotation and explicit scale, with no hidden best-fit solver.

## Evidence and deferred methods

Keep origin and lineage supplied by the data. Mixed reconstructed/generated data
must not become measured merely because it renders as a mesh. Preserve observed
versus inferred coverage when available, with unknown coverage otherwise. This
applies to World Labs Atlas/Marble as well as photogrammetry and scientific models.
No Atlas API or integration is assumed; those remain later input trials.

Keep source accuracy, control-fit residual, datum-operation accuracy, tracking
stability and representation approximation as separate statements with units,
frame, basis, method and validation status when supplied. An absent value means
unknown. A supplied RMS is preserved as supplied and is not automatically a
positional standard deviation. No sum, root-sum-square, covariance propagation,
confidence interval or global quality badge is computed by this increment.

Control-point fitting, ICP, robust estimators, scale estimation, uncertainty
propagation and analytical measurement need an explicit method decision with
representative data and constraints. Keep a deferred registration-method bead and
the existing scientific-tool review; do not slip a solver into mechanical work.
This is a bounded implementation sequence, not removal of scientific capability.

Engineering disposition: the portable evidence/solution contract and declared-map
workflow are ready for mechanical implementation. Empirical provider qualification
is still open. Scientific approval of automatic estimation/uncertainty methods is
neither required for the declared-map records nor implied by this review.
