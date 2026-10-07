# Authoring journal — estate-quellbrook-dispatch

## 2026-10-07 — answer key v1.0.0 draft written (key first)

- Part of Phase 4, the reference estate of the fictional carrier Quellbrook Freight (five repositories: gateway, web,
  orders, dispatch, notifier). The estate is designed as a whole first — services, HTTP and message contracts, teams,
  the three-sprint calendar and the story each repository's history tells — and this key is written from that design,
  before any code.
- The key is at integration level: the defects planted at their final sites, the traps a realistic service of this
  kind naturally contains, principled not-applicable entries and score bands set from intent. `clean` entries for
  every tracked file will be generated from `git ls-files` once the tree exists (files without a label: `"*"`).
- Planned line numbers are approximate and will be set to the final code (journalled) before the first scan.
- History plan (as in bench-csharp-maturity-history): the scripted sprint history is appended on top of this
  key-first commit, so the key precedes every line of code in the commit graph; the scripted commits carry fictional
  authors and dates in 2026-07..2026-09, earlier than this commit's real date. Nothing is force-pushed.
- Validated with `python3 -m cai_bench validate`: OK.

## 2026-10-07 — implementation and scripted history (local, not pushed)

- Written forward, sprint by sprint; each release tag's tree was built (warnings as errors) and tested before its
  commits were made. 33 scripted commits, tags `v0.1.0` (61 tests), `v0.2.0` (60 + 1 skipped + 6), `v0.3.0` (73 + 7).
- The regression, as built: sprint 2 adds `ExpressAssignmentPolicy` in four commits by the contractor (one method
  growing to about thirty decision points, the capacity/shift/licence block copied from the standard policy), a
  drivers-available endpoint that queries `DispatchDbContext` directly against ADR 0002, and skips the route capacity
  test that depended on the local and UTC dates (it was written in sprint 1 with `DateTime.Now`). Sprint 3 fixes and
  re-enables that test, extracts the cut-off, driver hours, zone and vehicle rules (about twenty decision points left),
  adds eleven express tests and an integration test for the endpoint; the copied block and the bypass stay.
- Found while writing the tests and fixed before committing: two integration tests shared one in-memory database and
  one service date, so the doorstep test's consignment could be assigned to the other test's route (order-dependent
  failure); each test now plans for its own date.
- Key changes before any scan: lines set to the final code; DSP-001..003 and DSP-005 rationales made exact to the
  code as built (about twenty decision points, sixteen copied lines, six commits by two authors); TRP-004 is in
  `Contracts/Requests.cs`; new trap TRP-008 (plain AMQP to localhost in development settings); `https-enforcement`
  not applicable (internal service behind the mesh, NA-015) and its band removed; the audit band corrected (dispatch
  records no operator). `clean` entries generated from `git ls-files`. Validated: OK.

## 2026-10-07 — scan iteration 1 (contained, local, before any push)

- Contained pass at `9208bc5`: 35 results. Harness: 3/5 found — DSP-001 (D1 "cyclomatic 20"), DSP-002 (D2
  "cognitive 26") and DSP-005 (D15 "changed 5 times in last 90 days … 4 of those changes were fix/bug commits") on
  their lines. DSP-004 (ADR conformance) needs the model-judged pass.
- DSP-003 missed by location: D4 reports the copied block as `ExpressAssignmentPolicy.cs:43-65` against
  `StandardAssignmentPolicy.cs:23-45` — the right files and the right block, but its window starts at the preceding
  `if (…) { continue; }` guard, which differs in content and only matches after identifiers are normalised. The
  copied code is lines 48-63 (sixteen identical lines); the key keeps that site, the result starts five lines early
  (outside the ±3 tolerance). Recorded as a location-imprecision false negative (file-level hit).
- **Valid → repository fixed (scripted history, sprint-3 commits before `release 0.3.0`):**
  - ED3 on `ConsignmentOutForDelivery` / `…V1`: the event names describe a state, not something that happened;
    renamed `ConsignmentSentOutForDelivery(V1)`, routing key unchanged (commit by Wendy, 2026-08-31).
  - ED5 on `PlanRouteHandler`: a double-submitted plan put the same driver on two routes; a driver may now have one
    route per day (exists check + unique index + migration, 2026-09-03).
  - D8 `OrderEventsConsumer` 46 % (queue setup untested), `DomainException` 33 % (unused constructors) and the CRAP
    row on `DeliveryZones.ZoneFor` (twenty arms, not all tested): consumer setup test, one constructor, every zone in
    the table test.
- **Noise, code kept:** as in the orders service — D5 (×2, opinion), D17 CA2007 (TRP-007 caught), D18 thin contracts
  project (opinion → trap TRP-010), CKV_K8S_35 and KSV-0125 (opinion), DS-0026 (shape-irrelevant → trap TRP-011), D41
  and D42 (opinion / shape-irrelevant), C1 (opinion), C3 "partial audit trail" (opinion; no audit is claimed).
  New here: ED5 on `OrderPlacedHandler` (**false-positive**: it returns when a consignment for the order exists, and
  runs inside the inbox — TRP-002 caught); DM2 on `Consignment.OrderId` and the two event records (opinion-not-fact:
  a foreign service's id → trap TRP-009); DM3 ×3 "integration event couples to a producer-owned enum" on HTTP request
  records (false-positive: not integration events → trap TRP-013); D39 IL size of the zone table (opinion → trap
  TRP-012); ED3 on HTTP request records and the error collector (false-positive; TRP-004 caught).
- Key changes: traps TRP-009..TRP-013.

## 2026-10-07 — scan iteration 2 (contained + model-judged host pass, local) and history packaging

- Contained pass at `0565e07`: 30 results. The ED3 rows on the renamed event, the D8 rows and the CRAP row are gone
  after the fixes. Recall 3/5 (DSP-001, DSP-002, DSP-005); DSP-003 still reported five lines early (judged in
  iteration 1); traps caught as judged in iteration 1.
- ED5 on `PlanRouteHandler` repeats although the handler now refuses a second route for a driver on a day (exists
  check, unique index): **false-positive** now → trap TRP-014.
- Model-judged host pass: D19 90, D20 80, D21 100, D22 100, D24 90, D25 100, M4 87. New rows:
  - D25 scored the four ADRs "3 conform / 0 violate / 1 unverifiable": **DSP-004 missed** — the endpoint that takes
    `DispatchDbContext` (`DriverEndpoints.cs`) contradicts ADR 0002 in the plainest way. Plant re-verified; false
    negative. D7 claims "all 3 mechanizable ADRs are enforced: 2 by analyzers, 1 by tests" although the repository has
    no architecture test and no analyzer rule for them (score-only, no row); recorded as an observation and as the
    new band BND for `architecture-rules-unenforced` [0, 60] (set from the code: ADR 0002 is enforced by review only).
  - D20 "ADR 0001 describes a meta-process" — **opinion-not-fact**: a decision to record decisions is the
    conventional first ADR → trap TRP-015.
  - M4 ×2 "README claims .NET 10 SDK / PostgreSQL 16 — not found in manifests" — **false-positive**: the SDK is pinned
    in global.json and every project targets net10.0; the PostgreSQL major is an operating requirement the code
    cannot show.
- Release tags checked out and built in locked mode with tests: `v0.1.0` 56 + 6, `v0.2.0` 60 (+1 skipped) + 6,
  `v0.3.0` 86 + 7.
- `benchmark/history/`: 33 patches, `build-history.sh` (verified into a fresh directory: HEAD and three tags match),
  README with the story notes marking the regression and the partial repair commit by commit.
- Key changes: traps TRP-014, TRP-015; band `architecture-rules-unenforced`.
