# Benchmark: estate-quellbrook-dispatch

**Theme.** Quellbrook Freight reference estate, dispatch service: a C# service (consignments, routes, drivers, vehicles) that consumes order events through an inbox and publishes dispatch events through an outbox; its scripted history carries the estate's REGRESSION (sprint 2: complexity, duplication, an ADR bypass, a skipped test and falling coverage) and a partial repair in sprint 3.

This repository is part of the scanner benchmark of the Code Assurance Initiative (`code-assurance-initiative/scanner-benchmark`). Its answer key, `benchmark/answer-key.json`, labels the defects that are really there (`must-fire`), sites that look like defects and are not (`must-not-fire`), code certified clean, concepts that do not apply, and expected score ranges. The key is at integration level: it labels the repository as a whole service, at the final commit, plus the one finding that lives only in history. `benchmark/journal.md` records how the repository was made and every scan judged on the way.

## The estate

Quellbrook Freight is a fictional parcel and freight carrier (the name was checked against companies, products and
trademarks; see `scanner-benchmark/docs/ESTATE-NAME.md`). Its operations platform is five repositories, owned by four
teams, that together form one system:

| Repository | What it is | Team | Trajectory over the three sprints |
|---|---|---|---|
| `estate-quellbrook-gateway` | TypeScript API gateway / BFF (Node 22, Fastify): operator authentication, routing, shipment view | Edge | steady |
| `estate-quellbrook-web` | TypeScript React + Vite operator console served by nginx | Edge | steady |
| `estate-quellbrook-orders` | C# order service: Order aggregate, domain events, transactional outbox | Orders | improving |
| `estate-quellbrook-dispatch` | C# dispatch service: consignments, routes, drivers, vehicles | Fleet | falling in sprint 2, partly repaired in sprint 3 |
| `estate-quellbrook-notifier` | C# worker sending e-mail and SMS through abstracted providers | Customer Comms | security incident introduced (sprint 2) and fixed (sprint 3) |

Operators use the web console, which calls the gateway; the gateway calls the order and dispatch services over HTTP
with its own service identity. The order service publishes `orders.*` events to a RabbitMQ topic exchange; dispatch
consumes them and publishes `dispatch.*` events; the notifier consumes both and notifies consignees. Every repository
owns its contracts: producers publish JSON Schemas and an AsyncAPI document under `contracts/`, consumers keep a
pinned copy of the versions they consume under `contracts/consumed/`. Each repository has its own Dockerfile,
Kubernetes manifests (`deploy/k8s/`), GitHub Actions workflows pinned by commit SHA, `docs/architecture.md` with C4
diagrams, ADRs and a `catalog-info.yaml`.

The sprints are two weeks long and identical across the estate: **sprint 1** 2026-07-27 – 2026-08-07 (release
`v0.1.0`), **sprint 2** 2026-08-10 – 2026-08-21 (`v0.2.0`), **sprint 3** 2026-08-24 – 2026-09-04 (`v0.3.0`). The
history is scripted and reproducible (`benchmark/history/`); its authors are fictional (`@example.invalid`).

## The sprint story (for a film director) — THE REGRESSION

The Fleet team (Kasper Nyholt, Wendy Achterberg, and a contractor, Dario Fenwick, in sprint 2) is the estate's
**falling** team: this repository holds the estate's regression.

- **Sprint 1 (v0.1.0).** A clean start: consignments created from `orders.order-placed.v1` through an inbox, routes,
  drivers and vehicles, standard assignment of a consignment to a route, starting a route and recording a delivery
  (`dispatch.*` events through an outbox). Small methods, tests for every rule.
- **Sprint 2 (v0.2.0) — the regression.** Same-day express delivery is rushed in. `ExpressAssignmentPolicy` is written
  as one large method, with the capacity and shift checks copied from the standard policy; two fixes in the same
  sprint add more branches. A "drivers available" endpoint for the dispatch board queries the database directly from
  the endpoint, against ADR 0002. A route-capacity test that fails on CI around midnight is skipped instead of fixed.
  The express policy ships with two tests: coverage falls.
- **Sprint 3 (v0.3.0) — a partial repair.** The skipped test is fixed (a fixed clock) and re-enabled; the express
  policy's cut-off and driver-hours rules are extracted into small tested types and more express scenarios are
  tested, so complexity and coverage recover part of the way. The copied block and the endpoint that bypasses the
  application layer stay.

Expected trend if each release tag is scanned: complexity, duplication and coverage **worse at v0.2.0 than at
v0.1.0**, **better at v0.3.0 than at v0.2.0**, and **still worse at v0.3.0 than at v0.1.0**. The commits are listed in
`benchmark/history/README.md`.

## Planted defects (`must-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| DSP-001 | `high-cyclomatic-complexity` | `src/Quellbrook.Dispatch.Domain/Assignment/ExpressAssignmentPolicy.cs:16` | ExpressAssignmentPolicy.Choose decides same-day express assignment in one method: service level, cut-off, route status and date, zone and vehicle fit, remaining capacity, driver activity, shift length and licence, driver hours, the capacity buffer and a scored tie-break. It grew to about thirty decision points in sprint 2; sprint 3's refactoring moved the cut-off, driver hours, zone and vehicle rules out, which brought it to about twenty: still above 15. |
| DSP-002 | `high-cognitive-complexity` | `src/Quellbrook.Dispatch.Domain/Assignment/ExpressAssignmentPolicy.cs:16` | The same method nests its rules three and four levels deep inside the candidate-route loop (continue/break inside nested conditions): hard to read and to change safely. |
| DSP-003 | `duplicated-code` | `src/Quellbrook.Dispatch.Domain/Assignment/ExpressAssignmentPolicy.cs:48-63` | The capacity, driver-activity, shift-length and licence checks were copied from StandardAssignmentPolicy into the express policy in sprint 2 (the same sixteen lines, the same order, nothing renamed) and were not extracted in sprint 3's refactoring: every change to how a route's capacity or a driver's eligibility is judged now has to be made twice. Keyed on the copy; the original block in StandardAssignmentPolicy.cs (lines 28-43) is the other half of the same defect and carries no clean label for this concept. |
| DSP-004 | `adr-conformance` | `docs/adr/0002-endpoints-call-application-handlers.md` | ADR 0002 (Accepted) decides that HTTP endpoints only call application handlers and that persistence is reached only through repositories. DriverEndpoints.cs (added in sprint 2 for the dispatch board) injects DispatchDbContext and queries drivers, vehicles and routes directly in the endpoint. Keyed on the ADR whose decision is broken; a result at the endpoint names the same violation. |
| DSP-005 | `churn-complexity-hotspot` | `src/Quellbrook.Dispatch.Domain/Assignment/ExpressAssignmentPolicy.cs` | ExpressAssignmentPolicy.cs is the most frequently changed production file of the last ninety days (created in sprint 2 and changed in six commits by two authors across sprints 2 and 3) and the most complex: change keeps landing on the riskiest code. Keyed on the file. |

## Traps (`must-not-fire`)

| Id | Concept | Site | Why |
|---|---|---|---|
| TRP-001 | `high-cyclomatic-complexity` | `src/Quellbrook.Dispatch.Domain/Consignments/DeliveryZones.cs:14` | DeliveryZones.ZoneFor maps a country and postal district to a delivery zone with one flat switch expression of twenty arms. Each arm is a table row, not a decision path a reader must follow; a lookup table is the clearest way to write it. |
| TRP-002 | `non-idempotent-message-handler` | `src/Quellbrook.Dispatch.Application/Intake/OrderPlacedHandler.cs:14-35` | The OrderPlaced handler is idempotent twice over: the consumer records each message id in an inbox table in the same transaction as the handler's changes and skips a message id it has seen, and the handler itself returns early when a consignment for the order already exists. |
| TRP-003 | `dual-write-without-outbox` | `src/Quellbrook.Dispatch.Infrastructure/Messaging/OutboxRelay.cs:23-56` | The outbox relay publishes stored messages and then marks them dispatched; it is the relay of a transactional outbox, not a dual write. |
| TRP-004 | `event-not-named-in-past-tense` | `src/Quellbrook.Dispatch.Api/Contracts/Requests.cs:54` | AssignConsignmentRequest is an HTTP request body, not an event. |
| TRP-005 | `hardcoded-credential` | `src/Quellbrook.Dispatch.Api/appsettings.json:15` | The broker address in appsettings.json is an amqps:// URI with host, port and virtual host only; the user name and password come from a Kubernetes Secret. |
| TRP-006 | `hardcoded-credential` | `deploy/k8s/deployment.yaml:56-70` | Database and broker credentials are read from Kubernetes Secrets (secretKeyRef) materialised by an ExternalSecret. |
| TRP-007 | `suppressed-diagnostic` | `.editorconfig:36-37` | CA2007 is switched off at the root for tests with its reason on the line above, and src/.editorconfig switches it back on for production code. |
| TRP-009 | `primitive-entity-identifier` | `src/Quellbrook.Dispatch.Domain/Consignments/Consignment.cs:38` | Consignment.OrderId holds the order service's identifier: a reference into another service, carried as the Guid that service publishes, not an identity of anything dispatch owns. Dispatch's own entities have strongly typed ids (ConsignmentId, RouteId, DriverId, VehicleId). |
| TRP-010 | `solution-structure` | `(repository)` | Quellbrook.Dispatch.Contracts is small on purpose: only the published event contracts, versioned on their own and unable to reference the domain. Repository-level: a scanner reports the solution's shape without a site. |
| TRP-011 | `missing-image-healthcheck` | `src/Quellbrook.Dispatch.Api/Dockerfile` | The image runs only on Kubernetes, which ignores a Dockerfile HEALTHCHECK; the probes are in deploy/k8s/deployment.yaml. |
| TRP-012 | `compiled-code-size` | `src/Quellbrook.Dispatch.Domain/Consignments/DeliveryZones.cs:10-14` | DeliveryZones.ZoneFor compiles to a large IL body because its switch expression is a twenty-row lookup table of postal districts; the size measures the table, not logic. |
| TRP-013 | `integration-event-leaks-domain-type` | `src/Quellbrook.Dispatch.Api/Contracts/Requests.cs:6` | RegisterDriverRequest is the HTTP request body of POST /fleet/drivers, deserialised inside the dispatch service itself, not an integration event another service consumes; using the domain's own LicenceCategory enum for it couples nothing across a boundary. (The same holds for the vehicle and delivery requests in this file.) |
| TRP-008 | `cleartext-transmission` | `src/Quellbrook.Dispatch.Api/appsettings.Development.json:9` | appsettings.Development.json points the broker at amqp://localhost for a developer's local RabbitMQ container; production configuration uses amqps. Plain AMQP to the loopback interface crosses no network. |

## Certified clean

157 `clean` entries, one per tracked file: files without a label are certified clean for every concept (`"*"`); a file that carries a plant or a trap is certified clean for every finding concept except the labelled ones and the concepts a result of those labels would restate.

## Not applicable

- `missing-text-alternative` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no images.
- `form-control-without-label` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no form controls.
- `page-structure-violation` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no pages.
- `non-keyboard-accessible-interaction` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no interactive markup.
- `invalid-aria-usage` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no ARIA attributes.
- `visual-and-motion-safety` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no styles or animation.
- `accessibility-checks-in-ci` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no user interface to check.
- `alt-text-quality` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no images.
- `link-and-button-text-quality` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no links or buttons.
- `heading-and-label-text-quality` — The dispatch service is an HTTP API with message consumers; it has no user interface: there are no headings or labels.
- `cross-site-scripting` — The API returns JSON only; it renders no HTML.
- `sensitive-data-in-browser-storage` — No browser code.
- `nondeterministic-event-fold` — The service stores state, not events.
- `mutable-persisted-event` — The service stores state, not events: there is no event store.
- `https-enforcement` — An internal API reachable only from the gateway's namespace, behind the cluster's service mesh, which encrypts and authenticates every connection with mutual TLS; the pod listens on plain HTTP to its sidecar by design. HTTPS redirection would break the gateway's calls and HSTS is a browser mechanism.

## Score bands

Bands were set from the intent of the code, before any scan, and are wide where a model judges.

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | `high-cyclomatic-complexity` | 40–95 | One method well above the threshold (DSP-001) among small ones. |
| BND-002 | `high-cognitive-complexity` | 40–95 | One deeply nested method (DSP-002) among flat ones. |
| BND-003 | `duplicated-code` | 40–95 | One copied block between the two assignment policies (DSP-003). |
| BND-004 | `test-coverage` | 55–95 | Coverage dropped in sprint 2 (the express policy shipped with two tests) and was partly restored in sprint 3. |
| BND-005 | `test-pyramid-distribution` | 50–100 | More unit than integration tests. |
| BND-006 | `dependencies-not-locked` | 80–100 | Central package management with committed lock files, restored in locked mode in CI. |
| BND-007 | `security-tooling-in-ci` | 60–100 | CodeQL on every pull request plus a vulnerable-package gate in CI. |
| BND-008 | `vulnerability-disclosure-policy` | 80–100 | SECURITY.md with a private reporting channel and response times. |
| BND-009 | `readme-quality` | 70–100 | README with purpose, build/run, configuration, testing, architecture and ownership sections. |
| BND-010 | `architecture-documentation` | 70–100 | docs/architecture.md with C4 diagrams in Mermaid, plus ADRs. |
| BND-011 | `folder-structure` | 80–100 | src/ and tests/ separation, one root namespace prefix. |
| BND-012 | `ci-build-and-test-pipeline` | 80–100 | CI builds and tests every push and pull request. |
| BND-013 | `observability` | 60–100 | Structured logging through ILogger, OpenTelemetry traces and metrics, health checks. |
| BND-014 | `deployment-rollback-safety` | 50–100 | Rolling updates by image digest with a rollback step in the deploy workflow. |
| BND-015 | `release-hygiene` | 40–100 | CHANGELOG with every release, <Version> 0.3.0 and tags v0.1.0..v0.3.0. |
| BND-016 | `build-provenance-and-signing` | 40–100 | Release images built by CI with provenance and SBOM attestations. |
| BND-017 | `network-egress-policy` | 60–100 | Default-deny NetworkPolicies with one allowance per real flow. |
| BND-018 | `workload-syscall-confinement` | 10–80 | seccomp RuntimeDefault, all capabilities dropped; no AppArmor or custom profiles. |
| BND-019 | `runtime-threat-detection-and-admission` | 0–40 | Pod Security Admission 'restricted' on the namespace; no runtime threat detection or signed-image admission. |
| BND-020 | `churn-complexity-hotspot` | 20–90 | One pronounced hotspot (DSP-005) that took most of the last two sprints' fixes. |
| BND-021 | `knowledge-concentration` | 30–100 | A team of two or three authors over six weeks; some files are naturally single-author. |
| BND-022 | `knowledge-freshness` | 70–100 | The whole history is recent and the authors are active. |
| BND-023 | `change-coupling` | 50–100 | Co-changes are explicit dependencies or tests changing with their subject. |
| BND-024 | `documentation-quality` | 60–100 | Model-judged. README, architecture overview and ADRs are clear and current. Wide band. |
| BND-025 | `adr-quality` | 60–100 | Model-judged. ADRs carry status, context, decision and two-sided consequences. Wide band. |
| BND-026 | `adr-conformance` | 20–85 | Model-judged. One of four ADRs is contradicted by an endpoint (DSP-004). Wide band. |
| BND-027 | `documentation-accuracy` | 50–100 | Model-judged. README and architecture describe the code as it is. Wide band. |
| BND-028 | `inconsistent-naming` | 60–100 | Model-judged. Consistent domain vocabulary. Wide band. |
| BND-029 | `low-value-comments` | 60–100 | Model-judged. Comments explain why, not what. Wide band. |
| BND-030 | `internal-api-inconsistency` | 60–100 | Model-judged. Endpoints and handlers follow one shape. Wide band. |
| BND-031 | `security-response-headers` | 70–100 | CSP, X-Content-Type-Options, frame, referrer and resource policy on every response. |
| BND-032 | `authorization-enforcement` | 70–100 | Every endpoint but health requires a named scope policy. |
| BND-033 | `inbound-input-validation` | 60–100 | Request bodies validated before they reach the domain. |
| BND-034 | `versioned-schema-migrations` | 80–100 | EF Core migrations, one per schema change. |
| BND-035 | `data-retention-policy` | 0–60 | Consignments keep a delivery postcode district and no names or contact data; no retention job. |
| BND-036 | `audit-trail` | 0–70 | No record of who planned, assigned or delivered beyond timestamps. |
| BND-037 | `data-subject-rights` | 0–60 | The service holds no direct identifiers of people other than drivers' display names. |
